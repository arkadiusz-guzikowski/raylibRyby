using System.Numerics;
using Raylib_cs;

namespace trening;

/// <summary>
/// Mini-gra "Strefa" (tryb raylib).
/// Zadanie gracza: utrzymać czerwony wskaźnik wewnątrz poruszającej się zielonej strefy.
/// SPACJA wciśnięta – wskaźnik płynie w górę względem strefy.
/// SPACJA puszczona – wskaźnik płynie w dół względem strefy.
/// Dotknięcie górnej lub dolnej krawędzi strefy kończy grę (GAME OVER).
/// Wynik naliczany jest za czas utrzymania się w strefie.
/// </summary>
internal class MinigraStrefa
{
    private enum Stan
    {
        Gra,
        Celowanie,
        Koniec,
        Zlapana
    }

    // Okno
    private const int SzerokoscOkna = 800;
    private const int WysokoscOkna = 600;

    // Szary tor (pionowy pasek po lewej stronie ekranu)
    private const int TorX = 100;
    private const int TorY = 50;
    private const int TorSzerokosc = 50;
    private const int TorWysokosc = 500;

    // Zielona strefa poruszająca się wewnątrz toru
    private const float StrefaSzerokosc = 42f;
    private const float StrefaWysokosc = 150f;
    private const float StrefaPredkoscMax = 800f;   // maksymalna prędkość losowego ruchu [px/s]
    private const float StrefaCzuloscZmiany = 4f; // jak szybko strefa zmienia kierunek (niższa = łagodniej)

    // Czerwony wskaźnik (kwadrat)
    private const float WskaznikRozmiar = 24f;
    private const float WskaznikPredkosc = 70f; // prędkość względem strefy [px/s]

    // Odległość do ryby (metry)
    private const float OdlegloscStartMin = 50f;    // losowy start: od
    private const float OdlegloscStartMax = 200f;   // losowy start: do
    private const float SzybkoscZwijania = 10f;     // metry/s zwijania, gdy SPACJA trzymana (ryba bliżej)
    private const float SzybkoscUcieczki = 5f;      // metry/s ucieczki, gdy SPACJA puszczona (hamulec)
    private const float MaksymalnaOdleglosc = 250f; // górny limit ucieczki ryby (przycina licznik)

    // Pasek odległości z ikoną ryby (dół ekranu)
    private const int PasekOdleglosciX = 200;          // lewy X paska
    private const int PasekOdleglosciY = 560;          // górny Y paska (pod grą)
    private const int PasekOdleglosciSzerokosc = 300;  // długość paska
    private const int PasekOdleglosciWysokosc = 15;    // grubość paska
    private const float PasekMetryMax = 250f;          // prawy koniec skali [m]

    // Mini-gra #2 – poziome celowanie pojawiające się losowo podczas holowania
    private const float CelowanieCoMinS = 10f;         // pojawia się po co najmniej tylu sekundach holu
    private const float CelowanieCoMaxS = 20f;         // ... a maksymalnie po tylu (średnio ~25 s)
    private const int PoziomTorX = 150;                // poziomy pasek: lewy X
    private const int PoziomTorY = 280;                // poziomy pasek: górny Y
    private const int PoziomTorSzerokosc = 500;        // szerokość poziomego paska
    private const int PoziomTorWysokosc = 50;          // wysokość poziomego paska
    private const float PoziomStrefaSzerokosc = 80f;   // szerokość zielonej strefy w poziomym pasku
    private const float PoziomWskaznikRozmiar = 20f;   // rozmiar poziomego wskaźnika
    private const float PoziomWskaznikPredkosc = 400f; // prędkość wskaźnika [px/s]

    // Dźwięk zwijania – tikanie syntezowane w kodzie (bez plików)
    private const int ProbkiNaSekunde = 22050;
    private const float CzasTiku = 0.02f;            // długość pojedynczego „cyku” [s]
    private const float CzestotliwoscTikGora = 1000f; // ruch w górę (SPACJA trzymana)
    private const float CzestotliwoscTikDol = 5000f;   // ruch w dół (SPACJA puszczona)
    private const float GlosnoscTiku = 0.20f;         // głośność (0.0–1.0)
    private const float CzasMiedzyTikami = 0.20f;     // ~8 cyknięć na sekundę

    // Dźwięki mini-gry #2 (celowanie) – syntezowane w kodzie
    private const float CzasTikuTrafienie = 0.12f;    // długość dźwięku trafienia [s]
    private const float CzestotliwoscTrafienie = 1568f; // wysoki ton sukcesu (G6)
    private const float CzasTikuPudlo = 0.18f;         // długość dźwięku pudła [s]
    private const float CzestotliwoscPudlo = 220f;     // niski „buzz” błędu (A3)

    // Kolory (jawne RGBA – niezależne od nazw stałych w bibliotece)
    private static readonly Color KolorTla = new(18, 18, 24, 255);
    private static readonly Color KolorTor = new(95, 95, 105, 255);
    private static readonly Color KolorTorKrawedz = new(55, 55, 65, 255);
    private static readonly Color KolorStrefa = new(50, 210, 90, 255);
    private static readonly Color KolorWskaznik = new(230, 40, 40, 255);
    private static readonly Color KolorTekst = new(255, 255, 255, 255);
    private static readonly Color KolorPrzyciemnienie = new(0, 0, 0, 160);
    private static readonly Color KolorPasekTlo = new(70, 70, 82, 255);
    private static readonly Color KolorRybaCialo = new(240, 190, 60, 255);
    private static readonly Color KolorRybaOgon = new(200, 140, 30, 255);
    private static readonly Color KolorRybaOko = new(15, 15, 15, 255);

    // Zmienne stanu gry
    private Stan _stan = Stan.Gra;
    private float _strefaY;       // górna krawędź zielonej strefy na ekranie
    private float _strefaPredkosc; // aktualna prędkość strefy [px/s], dodatnia = w dół
    private float _offset;        // przesunięcie wskaźnika od górnej krawędzi strefy
    private float _odleglosc;     // bieżąca odległość do ryby [m]
    private Sound _tikGora;       // efekt „cyk” przy ruchu w górę
    private Sound _tikDol;        // efekt „cyk” przy ruchu w dół
    private float _czasDoTiku;    // licznik do następnego cyknięcia
    private bool _poprzednioGora; // poprzedni kierunek ruchu wskaźnika
    private Sound _dzwiekTrafienie; // dźwięk trafienia w zieloną strefę (mini-gra #2)
    private Sound _dzwiekPudlo;     // dźwięk pudła (mini-gra #2)

    // Zmienne stanu mini-gry #2 (celowanie)
    private float _czasDoCelowania;   // pozostały czas do pojawienia się celowania
    private float _celStrefaX;        // lewy X zielonej strefy (poziomej)
    private float _celWskaznikX;      // aktualny X wskaźnika (poziomego)
    private float _celWskaznikPredkoscAktualna; // kierunek: + = w prawo, - = w lewo
    private bool _celBylWPrawo;       // czy wskaźnik dotarł już do prawej krawędzi

    /// <summary>
    /// Uruchamia pętlę gry (tworzy okno raylib i działa aż do jego zamknięcia).
    /// </summary>
    public void Run()
    {
        Raylib.InitWindow(SzerokoscOkna, WysokoscOkna, "Mini Gra: Strefa");
        Raylib.SetTargetFPS(60);

        // Inicjalizacja audio i synteza efektów tikania
        Raylib.InitAudioDevice();
        _tikGora = GenerujTik(CzestotliwoscTikGora, CzasTiku);
        _tikDol = GenerujTik(CzestotliwoscTikDol, CzasTiku);
        Raylib.SetSoundVolume(_tikGora, GlosnoscTiku);
        Raylib.SetSoundVolume(_tikDol, GlosnoscTiku);

        _dzwiekTrafienie = GenerujTik(CzestotliwoscTrafienie, CzasTikuTrafienie);
        _dzwiekPudlo = GenerujTik(CzestotliwoscPudlo, CzasTikuPudlo);
        Raylib.SetSoundVolume(_dzwiekTrafienie, GlosnoscTiku + 0.15f);
        Raylib.SetSoundVolume(_dzwiekPudlo, GlosnoscTiku);

        Reset();

        while (!Raylib.WindowShouldClose())
        {
            Aktualizuj();
            Rysuj();
        }

        // Sprzątanie zasobów audio i zamknięcie urządzenia dźwiękowego
        Raylib.UnloadSound(_tikGora);
        Raylib.UnloadSound(_tikDol);
        Raylib.UnloadSound(_dzwiekTrafienie);
        Raylib.UnloadSound(_dzwiekPudlo);
        Raylib.CloseAudioDevice();

        Raylib.CloseWindow();
    }

    /// <summary>
    /// Ustawia grę w stan początkowy (strefa i wskaźnik na środku).
    /// </summary>
    private void Reset()
    {
        _strefaY = TorY + (TorWysokosc - StrefaWysokosc) / 2f;
        _strefaPredkosc = 0f;
        _offset = (StrefaWysokosc - WskaznikRozmiar) / 2f;
        _odleglosc = Raylib.GetRandomValue((int)OdlegloscStartMin, (int)OdlegloscStartMax);
        _czasDoCelowania = LosowyCzasDoCelowania();
        _stan = Stan.Gra;
    }

    /// <summary>
    /// Aktualizuje logikę gry: ruch strefy, ruch wskaźnika, warunek przegranej, punktacja.
    /// </summary>
    private void Aktualizuj()
    {
        if (_stan == Stan.Koniec || _stan == Stan.Zlapana)
        {
            // Restart po wciśnięciu ENTER (z ekranu przegranej i wygranej)
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                Reset();
            }
            return;
        }

        float dt = Raylib.GetFrameTime();

        // Mini-gra #2 (celowanie) – zamrożenie logiki gry #1 na czas celowania
        if (_stan == Stan.Celowanie)
        {
            AktualizujCelowanie(dt);
            return;
        }

        // Dźwięk zwijania – tikanie w rytm ruchu wskaźnika
        bool ruchGora = Raylib.IsKeyDown(KeyboardKey.Space);
        if (ruchGora != _poprzednioGora)
        {
            _czasDoTiku = 0f;       // zmiana kierunku → reset rytmu
            _poprzednioGora = ruchGora;
        }
        _czasDoTiku += dt;
        if (_czasDoTiku >= CzasMiedzyTikami)
        {
            _czasDoTiku = 0f;
            if (ruchGora)
            {
                Raylib.PlaySound(_tikGora);
            }
            else
            {
                Raylib.PlaySound(_tikDol);
            }
        }

        // 1) Ruch zielonej strefy – płynny LOSOWY ruch („pływanie"):
        //    co klatkę losujemy docelową prędkość, a aktualna prędkość miękko do niej dąży.
        float celPredkosc = (Raylib.GetRandomValue(-1000, 1000) / 1000f) * StrefaPredkoscMax;
        _strefaPredkosc += (celPredkosc - _strefaPredkosc) * StrefaCzuloscZmiany * dt;
        _strefaY += _strefaPredkosc * dt;

        // Trzymaj strefę w granicach toru (zawrócenie przy krawędzi)
        float minY = TorY;
        float maxY = TorY + TorWysokosc - StrefaWysokosc;
        if (_strefaY <= minY)
        {
            _strefaY = minY;
            _strefaPredkosc = MathF.Abs(_strefaPredkosc); // zawróć w dół
        }
        else if (_strefaY >= maxY)
        {
            _strefaY = maxY;
            _strefaPredkosc = -MathF.Abs(_strefaPredkosc); // zawróć w górę
        }

        // 2) Ruch wskaźnika względem zielonej strefy
        if (Raylib.IsKeyDown(KeyboardKey.Space))
        {
            _offset -= WskaznikPredkosc * dt; // SPACJA trzymana → w górę strefy
        }
        else
        {
            _offset += WskaznikPredkosc * dt; // SPACJA puszczona → w dół strefy
        }

        // 3) Przegrana: wskaźnik dotknął lub wykroczył poza krawędź strefy
        float maxOffset = StrefaWysokosc - WskaznikRozmiar;
        if (_offset <= 0f || _offset >= maxOffset)
        {
            _offset = Math.Clamp(_offset, 0f, maxOffset);
            _stan = Stan.Koniec;
            return;
        }

        // 4) Zwijanie jak w prawdziwym wędkowaniu:
        //    SPACJA trzymana  → zwijasz linkę → ryba się zbliża (odległość maleje)
        //    SPACJA puszczona → działa hamulec → ryba powoli ucieka (odległość rośnie)
        if (ruchGora)
        {
            _odleglosc -= SzybkoscZwijania * dt;
        }
        else
        {
            _odleglosc += SzybkoscUcieczki * dt;
        }

        // Górny limit: ryba nie może uciec w nieskończoność
        if (_odleglosc > MaksymalnaOdleglosc)
        {
            _odleglosc = MaksymalnaOdleglosc;
        }

        // 5) Odległość 0 m → ryba złapana (wygrana)
        if (_odleglosc <= 0f)
        {
            _odleglosc = 0f;
            _stan = Stan.Zlapana;
            return;
        }

        // 6) Losowe pojawienie się mini-gry #2 (celowanie) w trakcie holowania
        _czasDoCelowania -= dt;
        if (_czasDoCelowania <= 0f)
        {
            _stan = Stan.Celowanie;
            RozpocznijCelowanie();
        }
    }

    /// <summary>
    /// Zwraca losowy czas (w sekundach) do pojawienia się mini-gry #2.
    /// </summary>
    private float LosowyCzasDoCelowania()
    {
        return Raylib.GetRandomValue((int)CelowanieCoMinS, (int)CelowanieCoMaxS);
    }

    /// <summary>
    /// Przygotowuje mini-grę #2: losowa zielona strefa oraz wskaźnik startujący z lewej.
    /// </summary>
    private void RozpocznijCelowanie()
    {
        float maxX = PoziomTorX + PoziomTorSzerokosc - PoziomStrefaSzerokosc;
        _celStrefaX = Raylib.GetRandomValue(PoziomTorX, (int)maxX);
        _celWskaznikX = PoziomTorX;
        _celWskaznikPredkoscAktualna = PoziomWskaznikPredkosc;
        _celBylWPrawo = false;
    }

    /// <summary>
    /// Aktualizuje mini-grę #2: wskaźnik jedzie w lewo/prawo (1 pełny przebieg),
    /// a SPACJA zatrzymuje go – trafienie zbliża rybę, pudło pozwala jej uciec.
    /// </summary>
    private void AktualizujCelowanie(float dt)
    {
        // Strzał – SPACJA zatrzymuje wskaźnik w bieżącym miejscu
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            float srodekWskaznika = _celWskaznikX + PoziomWskaznikRozmiar / 2f;
            bool trafienie = srodekWskaznika >= _celStrefaX
                             && srodekWskaznika <= _celStrefaX + PoziomStrefaSzerokosc;
            if (trafienie)
            {
                // Dźwięk sukcesu przy trafieniu w zieloną strefę
                Raylib.PlaySound(_dzwiekTrafienie);

                // Trafienie: ryba zbliża się o 5–10 m
                _odleglosc -= Raylib.GetRandomValue(5, 10);
                if (_odleglosc <= 0f)
                {
                    _odleglosc = 0f;
                    _stan = Stan.Zlapana;
                }
                else
                {
                    _stan = Stan.Gra;
                    _czasDoCelowania = LosowyCzasDoCelowania();
                }
            }
            else
            {
                WykonajPudlo();
            }
            return;
        }

        // Ruch wskaźnika w lewo i w prawo (ping-pong)
        _celWskaznikX += _celWskaznikPredkoscAktualna * dt;

        float lewa = PoziomTorX;
        float prawa = PoziomTorX + PoziomTorSzerokosc - PoziomWskaznikRozmiar;
        if (_celWskaznikX >= prawa)
        {
            _celWskaznikX = prawa;
            _celWskaznikPredkoscAktualna = -MathF.Abs(_celWskaznikPredkoscAktualna); // zawróć w lewo
            _celBylWPrawo = true;
        }
        else if (_celWskaznikX <= lewa)
        {
            _celWskaznikX = lewa;
            _celWskaznikPredkoscAktualna = MathF.Abs(_celWskaznikPredkoscAktualna); // zawróć w prawo

            // Pełny przebieg (w prawo i z powrotem) bez strzału → automatyczne pudło
            if (_celBylWPrawo)
            {
                WykonajPudlo();
            }
        }
    }

    /// <summary>
    /// Rozstrzyga pudło: ryba zawsze ucieka o 20–50 m, a z 50% szans zerwie linkę (GAME OVER).
    /// </summary>
    private void WykonajPudlo()
    {
        // Dźwięk błędu przy pudle
        Raylib.PlaySound(_dzwiekPudlo);

        _odleglosc += Raylib.GetRandomValue(20, 50);
        if (_odleglosc > MaksymalnaOdleglosc)
        {
            _odleglosc = MaksymalnaOdleglosc;
        }

        // 50% szans, że ryba się spina i zrywa linkę
        if (Raylib.GetRandomValue(0, 1) == 1)
        {
            _stan = Stan.Koniec;
        }
        else
        {
            _stan = Stan.Gra;
            _czasDoCelowania = LosowyCzasDoCelowania();
        }
    }

    /// <summary>
    /// Rysuje całą scenę: tor, zieloną strefę, wskaźnik, wynik oraz ekran końca gry.
    /// </summary>
    private void Rysuj()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(KolorTla);

        // Szary tor (pionowy pasek) z obrysem
        Raylib.DrawRectangle(TorX, TorY, TorSzerokosc, TorWysokosc, KolorTor);
        Raylib.DrawRectangleLines(TorX, TorY, TorSzerokosc, TorWysokosc, KolorTorKrawedz);

        // Zielona strefa (wyśrodkowana poziomo w torze)
        float strefaX = TorX + (TorSzerokosc - StrefaSzerokosc) / 2f;
        Raylib.DrawRectangle((int)strefaX, (int)_strefaY, (int)StrefaSzerokosc, (int)StrefaWysokosc, KolorStrefa);

        // Czerwony wskaźnik – zawsze wewnątrz strefy
        float wskaznikX = TorX + (TorSzerokosc - WskaznikRozmiar) / 2f;
        float wskaznikY = _strefaY + _offset;
        Raylib.DrawRectangle((int)wskaznikX, (int)wskaznikY, (int)WskaznikRozmiar, (int)WskaznikRozmiar, KolorWskaznik);

        // Podpowiedź sterowania (lewy górny róg)
        Raylib.DrawText("SPACJA - w gore | puszczenie - w dol", 30, 10, 30, KolorTekst);

        RysujPasekOdleglosci();

        // Mini-gra #2 – poziome celowanie (nakładka na zamrożoną grę #1)
        if (_stan == Stan.Celowanie)
        {
            Raylib.DrawRectangle(0, 0, SzerokoscOkna, WysokoscOkna, KolorPrzyciemnienie);

            string naglowek = "CELOWANIE! Wcisnij SPACJE w zielonej strefie!";
            int xNaglowek = (SzerokoscOkna - Raylib.MeasureText(naglowek, 26)) / 2;
            Raylib.DrawText(naglowek, xNaglowek, PoziomTorY - 45, 26, KolorTekst);

            // Poziomy szary pasek z obrysem
            Raylib.DrawRectangle(PoziomTorX, PoziomTorY, PoziomTorSzerokosc, PoziomTorWysokosc, KolorTor);
            Raylib.DrawRectangleLines(PoziomTorX, PoziomTorY, PoziomTorSzerokosc, PoziomTorWysokosc, KolorTorKrawedz);

            // Zielona strefa w losowym miejscu poziomego paska
            Raylib.DrawRectangle((int)_celStrefaX, PoziomTorY, (int)PoziomStrefaSzerokosc, PoziomTorWysokosc, KolorStrefa);

            // Czerwony wskaźnik jadący w lewo/prawo
            int wskY = PoziomTorY + (PoziomTorWysokosc - (int)PoziomWskaznikRozmiar) / 2;
            Raylib.DrawRectangle((int)_celWskaznikX, wskY, (int)PoziomWskaznikRozmiar, (int)PoziomWskaznikRozmiar, KolorWskaznik);
        }

        // Ekran końca gry
        if (_stan == Stan.Koniec)
        {
            Raylib.DrawRectangle(0, 0, SzerokoscOkna, WysokoscOkna, KolorPrzyciemnienie);

            const int rozmiarGameOver = 64;
            string gameOver = "GAME OVER";
            int xGo = (SzerokoscOkna - Raylib.MeasureText(gameOver, rozmiarGameOver)) / 2;
            Raylib.DrawText(gameOver, xGo, 200, rozmiarGameOver, KolorWskaznik);

            string podpowiedz = "Nacisnij ENTER, aby zagrac ponownie";
            int xHint = (SzerokoscOkna - Raylib.MeasureText(podpowiedz, 26)) / 2;
            Raylib.DrawText(podpowiedz, xHint, 310, 26, KolorTekst);
        }
        else if (_stan == Stan.Zlapana)
        {
            Raylib.DrawRectangle(0, 0, SzerokoscOkna, WysokoscOkna, KolorPrzyciemnienie);

            const int rozmiarWygrana = 48;
            string wygrana = "RYBA ZLAPANA!";
            int xWy = (SzerokoscOkna - Raylib.MeasureText(wygrana, rozmiarWygrana)) / 2;
            Raylib.DrawText(wygrana, xWy, 210, rozmiarWygrana, KolorStrefa);

            string podpowiedz = "Nacisnij ENTER, aby zagrac ponownie";
            int xHint = (SzerokoscOkna - Raylib.MeasureText(podpowiedz, 26)) / 2;
            Raylib.DrawText(podpowiedz, xHint, 310, 26, KolorTekst);
        }

        Raylib.EndDrawing();
    }

    /// <summary>
    /// Rysuje na dole ekranu poziomy pasek odległości z ikoną ryby.
    /// Lewy koniec paska = 0 m (ryba przy graczu), prawy = PasekMetryMax.
    /// Im mniejsza odległość do ryby, tym rybka bliżej lewej strony.
    /// </summary>
    private void RysujPasekOdleglosci()
    {
        // Proporcja pozostałej odległości (0..1): 1 = daleko (250 m), 0 = złapana
        float ratio = Math.Clamp(_odleglosc / PasekMetryMax, 0f, 1f);

        // Licznik odległości nad paskiem (wyśrodkowany nad skalą z rybką)
        string tekstOdleglosci = $"Odleglosc: {(int)_odleglosc} m";
        int xOdleglosc = (PasekOdleglosciX + PasekOdleglosciSzerokosc / 2) - Raylib.MeasureText(tekstOdleglosci, 24) / 2;
        Raylib.DrawText(tekstOdleglosci, xOdleglosc, PasekOdleglosciY - 46, 24, KolorTekst);

        // Tło paska
        Raylib.DrawRectangle(PasekOdleglosciX, PasekOdleglosciY,
            PasekOdleglosciSzerokosc, PasekOdleglosciWysokosc, KolorPasekTlo);

        // Pozycja rybki: 0 m → lewy koniec, 250 m → prawy koniec
        int fishX = PasekOdleglosciX + (int)(ratio * PasekOdleglosciSzerokosc);
        int centerY = PasekOdleglosciY + PasekOdleglosciWysokosc / 2;
        int bob = (int)(MathF.Sin((float)Raylib.GetTime() * 5f) * 3f);
        centerY += bob;

        // Ogon (trójkąt po prawej stronie ciała)
        Raylib.DrawTriangle(
            new Vector2(fishX + 10, centerY),
            new Vector2(fishX + 20, centerY - 9),
            new Vector2(fishX + 20, centerY + 9),
            KolorRybaOgon);

        // Ciało (elipsa)
        Raylib.DrawEllipse(fishX, centerY, 14f, 9f, KolorRybaCialo);

        // Oko (po lewej stronie ciała – rybka „patrzy” w stronę gracza)
        Raylib.DrawCircle(fishX - 7, centerY - 3, 2.5f, KolorRybaOko);

        // Podpisy skali nad paskiem
        Raylib.DrawText("0 m", PasekOdleglosciX, PasekOdleglosciY - 22, 16, KolorTekst);
        string etykietaMax = $"{(int)PasekMetryMax} m";
        int xEtykMax = PasekOdleglosciX + PasekOdleglosciSzerokosc + 6 - Raylib.MeasureText(etykietaMax, 16);
        Raylib.DrawText(etykietaMax, xEtykMax, PasekOdleglosciY - 22, 16, KolorTekst);
    }

    /// <summary>
    /// Generuje efekt o zadanej częstotliwości i długości, a następnie ładuje go jako Sound z pamięci.
    /// </summary>
    private static Sound GenerujTik(float czestotliwosc, float czasTiku)
    {
        byte[] wav = UtworzWavTik(czestotliwosc, czasTiku);
        Wave fala = Raylib.LoadWaveFromMemory(".wav", wav);
        Sound dzwiek = Raylib.LoadSoundFromWave(fala);
        Raylib.UnloadWave(fala);
        return dzwiek;
    }

    /// <summary>
    /// Tworzy w pamięci plik WAV (PCM 16-bit, mono, 22050 Hz) z krótkim tonem
    /// o szybkim zaniku obwiedni – brzmi jak suche, mechaniczne „cyk”.
    /// </summary>
    private static byte[] UtworzWavTik(float czestotliwosc, float czasTiku)
    {
        int liczbaProbek = (int)(czasTiku * ProbkiNaSekunde);
        byte[] wav = new byte[44 + liczbaProbek * 2];

        // Nagłówek WAV (44 bajty) – RIFF/WAVE, fmt PCM mono 16-bit
        wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
        int rozmiarPliku = 36 + liczbaProbek * 2;
        wav[4] = (byte)(rozmiarPliku & 0xFF);
        wav[5] = (byte)((rozmiarPliku >> 8) & 0xFF);
        wav[6] = (byte)((rozmiarPliku >> 16) & 0xFF);
        wav[7] = (byte)((rozmiarPliku >> 24) & 0xFF);
        wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';
        wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
        wav[16] = 16; wav[17] = 0; wav[18] = 0; wav[19] = 0;       // rozmiar bloku fmt
        wav[20] = 1; wav[21] = 0;                                    // PCM = 1
        wav[22] = 1; wav[23] = 0;                                    // mono = 1 kanał
        wav[24] = (byte)(ProbkiNaSekunde & 0xFF);
        wav[25] = (byte)((ProbkiNaSekunde >> 8) & 0xFF);
        wav[26] = (byte)((ProbkiNaSekunde >> 16) & 0xFF);
        wav[27] = (byte)((ProbkiNaSekunde >> 24) & 0xFF);           // sample rate
        int bajtRate = ProbkiNaSekunde * 2;
        wav[28] = (byte)(bajtRate & 0xFF);
        wav[29] = (byte)((bajtRate >> 8) & 0xFF);
        wav[30] = (byte)((bajtRate >> 16) & 0xFF);
        wav[31] = (byte)((bajtRate >> 24) & 0xFF);                 // byte rate
        wav[32] = 2; wav[33] = 0;                                    // block align
        wav[34] = 16; wav[35] = 0;                                   // 16 bitów na próbkę
        wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
        int rozmiarDanych = liczbaProbek * 2;
        wav[40] = (byte)(rozmiarDanych & 0xFF);
        wav[41] = (byte)((rozmiarDanych >> 8) & 0xFF);
        wav[42] = (byte)((rozmiarDanych >> 16) & 0xFF);
        wav[43] = (byte)((rozmiarDanych >> 24) & 0xFF);

        // Próbki: sinus z szybkim zanikiem → suchy, mechaniczny „cyk”
        const float dwaPi = 2f * MathF.PI;
        for (int i = 0; i < liczbaProbek; i++)
        {
            float t = (float)i / ProbkiNaSekunde;
            float obwiednia = MathF.Exp(-t * 160f);
            float probka = MathF.Sin(dwaPi * czestotliwosc * t) * obwiednia;
            short wartosc = (short)(probka * short.MaxValue * 0.9f);
            wav[44 + i * 2] = (byte)(wartosc & 0xFF);
            wav[45 + i * 2] = (byte)((wartosc >> 8) & 0xFF);
        }
        return wav;
    }
}
