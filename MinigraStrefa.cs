using System.Numerics;

using Raylib_cs;



namespace trening;



/// <summary>

/// Mini-gra "Strefa" (tryb raylib).

/// Zadanie gracza: utrzymac czerwony wskaznik wewnatrz poruszajacej sie zielonej strefy.

/// SPACJA wcisnieta - wskaznik plynie w gore wzgledem strefy.

/// SPACJA puszczona - wskaznik plynie w dol wzgledem strefy.

/// Dotkniecie gornej lub dolnej krawedzi strefy konczy gre (GAME OVER).

/// Wynik naliczany jest za czas utrzymania sie w strefie.

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



    // Zielona strefa poruszajaca sie wewnatrz toru

    private const float StrefaSzerokosc = 42f;

    private const float StrefaWysokosc = 200f;

    private const float StrefaPredkoscMax = 2000f;   // maksymalna predkosc losowego ruchu [px/s]

    private const float StrefaCzuloscZmiany = 2f; // jak szybko strefa zmienia kierunek (nizsza = lagodniej)

    // Strefa ryzyka (zolta obwodka): wysoki pas powyzej i ponizej zielonej.
    private const float StrefaRyzykoWysokosc = 200f;        // grubosc pojedynczego zoltego pasa [px]
    private const float SzybkoscZwijaniaBonusZolty = 1.40f; // zwijanie +40% gdy wskaznik w zoltej
    private const float CzasProgZerwaniaZolty = 2.0f;      // sekundy ciaglego pobytu w zoltej do zerwania linki



    // Czerwony wskaznik (kwadrat)

    private const float WskaznikRozmiar = 24f;

    private const float WskaznikPredkosc = 70f; // predkosc wzgledem strefy [px/s]



    // Odleglosc do ryby (metry)

    private const float OdlegloscStartMin = 50f;    // losowy start: od

    private const float OdlegloscStartMax = 200f;   // losowy start: do

    private const float SzybkoscZwijania = 10f;     // metry/s zwijania, gdy SPACJA trzymana (ryba bliej)

    private const float SzybkoscUcieczki = 5f;      // metry/s ucieczki, gdy SPACJA puszczona (hamulec)

    private const float MaksymalnaOdleglosc = 250f; // gorny limit ucieczki ryby (przycina licznik)



    // Masa ryby i trudnosc (im ciezsza ryba, tym trudniejszy poziom)

    private const int MasaMinKg = 1;                // najmniejsza mozliwa ryba [kg]

    private const int MasaMaxKg = 40;               // najwieksza mozliwa ryba [kg]

        private const float StalaRzadkosciKg = 0.8f; // im wyzsza, tym rzadsze ryby 25-40 kg (trofea)
    private const int MasaStartKg = 15;             // masa pierwszej ryby w sesji (do testow)



    // Zwijanie/ucieczka zalezne od masy: dla 40 kg zwijanie najwolniejsze, ucieczka najszybsza

    private const float SzybkoscZwijaniaMin = 14f;   // najwolniejsze zwijanie (ryba 40 kg) [m/s]

    private const float SzybkoscUcieczkiMax = 8f;   // najszybsza ucieczka (ryba 40 kg) [m/s]



    // Pudlo w mini-grze #2: bazowy odrzut (lekka ryba) i zaostrzony (ciezka ryba)

    private const int PudloOdrzutMin = 20;          // bazowy odrzut pudla: od [m]

    private const int PudloOdrzutMax = 50;          // bazowy odrzut pudla: do [m]

    private const int PudloOdrzutCiezkiejMin = 35;  // odrzut pudla dla ryby 40 kg: od [m]

    private const int PudloOdrzutCiezkiejMax = 75;  // odrzut pudla dla ryby 40 kg: do [m]



    // Mini-gra #2 (celowanie) dla ciezkiej ryby: szybszy wskaznik i wezsza strefa trafienia

    private const float WskaznikPredkoscMax = 130f; // najwieksza predkosc wskaznika (ryba 40 kg) [px/s]

    private const float SzerokoscStrefyMin = 65f;   // najwezsza strefa trafienia (ryba 40 kg) [px]



    // Pasek odleglosci z ikona ryby (dol ekranu)

    private const int PasekOdleglosciX = 200;          // lewy X paska

    private const int PasekOdleglosciY = 560;          // gorny Y paska (pod gra)

    private const int PasekOdleglosciSzerokosc = 300;  // dlugosc paska

    private const int PasekOdleglosciWysokosc = 15;    // grubosc paska

    private const float PasekMetryMax = 250f;          // prawy koniec skali [m]



    // Mini-gra #2 - poziome celowanie pojawiajace sie losowo podczas holowania

    private const float CelowanieCoMinS = 10f;         // pojawia sie po co najmniej tylu sekundach holu

    private const float CelowanieCoMaxS = 20f;         // ... a maksymalnie po tylu (srednio ~25 s)

    private const int PoziomTorX = 150;                // poziomy pasek: lewy X

    private const int PoziomTorY = 280;                // poziomy pasek: gorny Y

    private const int PoziomTorSzerokosc = 500;        // szerokosc poziomego paska

    private const int PoziomTorWysokosc = 50;          // wysokosc poziomego paska

    private const float PoziomStrefaSzerokosc = 80f;   // szerokosc zielonej strefy w poziomym pasku

    private const float PoziomWskaznikRozmiar = 20f;   // rozmiar poziomego wskaznika

    private const float PoziomWskaznikPredkosc = 400f; // predkosc wskaznika [px/s]

    private const float PoziomWskaznikPredkoscMax = 620f; // najwieksza predkosc wskaznika celowania (ryba 40 kg) [px/s]



    // Dzwiek zwijania - tikanie syntezowane w kodzie (bez plikow)

    private const int ProbkiNaSekunde = 22050;

    private const float CzasTiku = 0.02f;            // dlugosc pojedynczego "cyku" [s]

    private const float CzestotliwoscTikGora = 1000f; // ruch w gore (SPACJA trzymana)

    private const float CzestotliwoscTikDol = 5000f;   // ruch w dol (SPACJA puszczona)

    private const float GlosnoscTiku = 0.20f;         // glosnosc (0.0-1.0)

    private const float CzasMiedzyTikami = 0.20f;     // ~8 cyknien na sekunde



    // Dzwieki mini-gry #2 (celowanie) - syntezowane w kodzie

    private const float CzasTikuTrafienie = 0.12f;    // dlugosc dzwieku trafienia [s]

    private const float CzestotliwoscTrafienie = 1568f; // wysoki ton sukcesu (G6)

    private const float CzasTikuPudlo = 0.18f;         // dlugosc dzwieku pudla [s]

    private const float CzestotliwoscPudlo = 220f;     // niski "buzz" bledu (A3)



    // Kolory (jawne RGBA - niezalezne od nazw stalych w bibliotece)

    private static readonly Color KolorTla = new(18, 18, 24, 255);

    private static readonly Color KolorTor = new(95, 95, 105, 255);

    private static readonly Color KolorTorKrawedz = new(55, 55, 65, 255);

    private static readonly Color KolorStrefa = new(50, 210, 90, 255);
    private static readonly Color KolorStrefaRyzyko = new(245, 200, 40, 255); // zolty pas ryzyka

    private static readonly Color KolorWskaznik = new(230, 40, 40, 255);

    private static readonly Color KolorTekst = new(255, 255, 255, 255);

    private static readonly Color KolorPrzyciemnienie = new(0, 0, 0, 160);

    private static readonly Color KolorPasekTlo = new(70, 70, 82, 255);

    private static readonly Color KolorRybaCialo = new(240, 190, 60, 255);

    private static readonly Color KolorRybaOgon = new(200, 140, 30, 255);

    private static readonly Color KolorRybaOko = new(15, 15, 15, 255);



    // Zmienne stanu gry

    private Stan _stan = Stan.Gra;

    private float _strefaY;       // gorna krawedz zielonej strefy na ekranie

    private float _strefaPredkosc; // aktualna predkosc strefy [px/s], dodatnia = w dol

    private float _wskaznikY;      // gorna krawedz czerwonego wskaznika w obrebie toru [y]
    private float _czasWRyzykuZoltej; // sekundy ciaglego pobytu wskaznika w zoltej strefie

    private float _odleglosc;     // biezaca odleglosc do ryby [m]

    private float _masaRyby;      // masa biezacej ryby [kg] (1.000..40.000)

    private float _rekordKg;      // najciezsza zlapana ryba w tej sesji [kg]

    private bool _czyNowyRekord;  // czy ostatnio zlapana ryba pobila rekord (komunikat na ekranie)

    private Sound _tikGora;       // efekt "cyk" przy ruchu w gore

    private Sound _tikDol;        // efekt "cyk" przy ruchu w dol

    private float _czasDoTiku;    // licznik do nastepnego cykniecia

    private bool _poprzednioGora; // poprzedni kierunek ruchu wskaznika

    private Sound _dzwiekTrafienie; // dzwiek trafienia w zielona strefe (mini-gra #2)

    private Sound _dzwiekPudlo;     // dzwiek pudla (mini-gra #2)



    // Zmienne stanu mini-gry #2 (celowanie)

    private float _czasDoCelowania;   // pozostaly czas do pojawienia sie celowania

    private float _celStrefaX;        // lewy X zielonej strefy (poziomej)

    private float _celWskaznikX;      // aktualny X wskaznika (poziomego)

    private float _celWskaznikPredkoscAktualna; // kierunek: + = w prawo, - = w lewo

    private bool _celBylWPrawo;       // czy wskaznik dotarl ju do prawej krawedzi



    /// <summary>

    /// Uruchamia petle gry (tworzy okno raylib i dziala a do jego zamkniecia).

    /// </summary>

    public void Run()

    {

        Raylib.InitWindow(SzerokoscOkna, WysokoscOkna, "Mini Gra: Strefa");

        Raylib.SetTargetFPS(60);



        // Inicjalizacja audio i synteza efektow tikania

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



        // Sprzatanie zasobow audio i zamkniecie urzadzenia dzwiekowego

        Raylib.UnloadSound(_tikGora);

        Raylib.UnloadSound(_tikDol);

        Raylib.UnloadSound(_dzwiekTrafienie);

        Raylib.UnloadSound(_dzwiekPudlo);

        Raylib.CloseAudioDevice();



        Raylib.CloseWindow();

    }



    /// <summary>

    /// Ustawia gre w stan poczatkowy (strefa i wskaznik na srodku).

    /// </summary>

    private void Reset()

    {

        // Kazda nowa runda = nowa ryba o losowej masie 1-40 kg (im ciezsza, tym trudniejszy poziom)

        _masaRyby = LosujMaseRyby();

        _czyNowyRekord = false; // komunikat "nowy rekord" tylko raz, bezpsrednio po zlowieniu

        _strefaY = TorY + (TorWysokosc - StrefaWysokosc) / 2f;

        _wskaznikY = TorY + (TorWysokosc - WskaznikRozmiar) / 2f;

        _strefaPredkosc = 0f;

        _czasWRyzykuZoltej = 0f;

        _odleglosc = Raylib.GetRandomValue((int)OdlegloscStartMin, (int)OdlegloscStartMax);

        _czasDoCelowania = LosowyCzasDoCelowania();

        _stan = Stan.Gra;

    }



    /// <summary>

    /// Aktualizuje logike gry: ruch strefy, ruch wskaznika, warunek przegranej, punktacja.

    /// </summary>

    private void Aktualizuj()

    {

        if (_stan == Stan.Koniec || _stan == Stan.Zlapana)

        {

            // Restart po wciL>nieciu ENTER (z ekranu przegranej i wygranej)

            if (Raylib.IsKeyPressed(KeyboardKey.Enter))

            {

                Reset();

            }

            return;

        }



        float dt = Raylib.GetFrameTime();



        // Mini-gra #2 (celowanie) - zamroenie logiki gry #1 na czas celowania

        if (_stan == Stan.Celowanie)

        {

            AktualizujCelowanie(dt);

            return;

        }



        // Dzwiek zwijania - tikanie w rytm ruchu wskaznika

        bool ruchGora = Raylib.IsKeyDown(KeyboardKey.Space);

        if (ruchGora != _poprzednioGora)

        {

            _czasDoTiku = 0f;       // zmiana kierunku - reset rytmu

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



        // 1) Ruch zielonej strefy - plynny LOSOWY ruch ("plywanie"):

        //    co klatke losujemy docelowa predkosc, a aktualna predkosc miekko do niej day.

        float celPredkosc = (Raylib.GetRandomValue(-1000, 1000) / 1000f) * StrefaPredkoscMax;

        _strefaPredkosc += (celPredkosc - _strefaPredkosc) * StrefaCzuloscZmiany * dt;

        _strefaY += _strefaPredkosc * dt;



        // Trzymaj strefe w granicach toru (zawrocenie przy krawedzi)

        float minY = TorY;

        float maxY = TorY + TorWysokosc - StrefaWysokosc;

        if (_strefaY <= minY)

        {

            _strefaY = minY;

            _strefaPredkosc = MathF.Abs(_strefaPredkosc); // zawroc w dol

        }

        else if (_strefaY >= maxY)

        {

            _strefaY = maxY;

            _strefaPredkosc = -MathF.Abs(_strefaPredkosc); // zawroc w gore

        }



        // 2) Ruch wskaznika wzgledem zielonej strefy - ciezsza ryba = szybszy dryf (trudniej utrzymac go w strefie)

        float aktualnaPredkoscWskaznika = AktualnaPredkoscWskaznika();

        if (Raylib.IsKeyDown(KeyboardKey.Space))

        {

            _wskaznikY -= aktualnaPredkoscWskaznika * dt; // SPACJA trzymana - czerwony w gore toru

        }

        else

        {

            _wskaznikY += aktualnaPredkoscWskaznika * dt; // SPACJA puszczona - czerwony w dol toru

        }



        // Trzymaj czerwony wskaznik w obrebie szarego toru (jasna granica)

        float wMinY = TorY;

        float wMaxY = TorY + TorWysokosc - WskaznikRozmiar;

        _wskaznikY = Math.Clamp(_wskaznikY, wMinY, wMaxY);



        // 3) Strefy: zielona = cel (zwijanie), zolta = ryzyko (+40% zwijania, kumulacja zerwania),
        //    poza zoltym pasem = natychmiastowe zerwanie linki.

        float srodekZielonej = _strefaY + StrefaWysokosc / 2f;

        float srodekWskaznika = _wskaznikY + WskaznikRozmiar / 2f;

        float odlegloscOdSrodka = MathF.Abs(srodekWskaznika - srodekZielonej);

        float polowaZielona = StrefaWysokosc / 2f;                  // do tej odleglosci = w zielonej

        float granicaZolta = polowaZielona + StrefaRyzykoWysokosc;  // do tej odleglosci = w zoltej



        if (odlegloscOdSrodka <= polowaZielona)

        {

            // W zielonej - zwijanie normalne (bez bonusu), reset licznika ryzyka

            _czasWRyzykuZoltej = 0f;

        }

        else if (odlegloscOdSrodka <= granicaZolta)

        {

            // W zoltej: kumuluje ryzyko zerwania linki

            _czasWRyzykuZoltej += dt;

            if (_czasWRyzykuZoltej >= CzasProgZerwaniaZolty)

            {

                _stan = Stan.Koniec;

                return;

            }

        }

        else

        {

            // Calkowicie poza zielona i zolta - natychmiastowe zerwanie linki

            _stan = Stan.Koniec;

            return;

        }



        // 4) Zwijanie: baza zalezy od sterowania, a w zoltej strefie zwijanie idzie szybciej o 40%

        bool wStreFieZoltej = (odlegloscOdSrodka > polowaZielona);

        float predkoscZwijania = AktualnaSzybkoscZwijania();

        if (wStreFieZoltej)

        {

            predkoscZwijania *= SzybkoscZwijaniaBonusZolty;

        }



        if (ruchGora)

        {

            _odleglosc -= predkoscZwijania * dt;

        }

        else

        {

            _odleglosc += AktualnaSzybkoscUcieczki() * dt;

        }



        // Gorny limit: ryba nie moze uciec w nieskonczonosc

        if (_odleglosc > MaksymalnaOdleglosc)

        {

            _odleglosc = MaksymalnaOdleglosc;

        }



        // 5) Odleglsc 0 m - ryba zlapana (wygrana)

        if (_odleglosc <= 0f)

        {

            _odleglosc = 0f;

            ZlapRybe();

            return;

        }



        // 6) Losowe pojawienie sie mini-gry #2 (celowanie) w trakcie holowania

        _czasDoCelowania -= dt;

        if (_czasDoCelowania <= 0f)

        {

            _stan = Stan.Celowanie;

            RozpocznijCelowanie();

        }

    }



    /// <summary>

    /// Zwraca losowy czas (w sekundach) do pojawienia sie mini-gry #2.

    /// </summary>

    private float LosowyCzasDoCelowania()

    {

        return Raylib.GetRandomValue((int)CelowanieCoMinS, (int)CelowanieCoMaxS);

    }



    /// <summary>

    /// Przygotowuje mini-gre #2: losowa zielona strefa oraz wskaznik startujacy z lewej.

    /// </summary>

    private void RozpocznijCelowanie()

    {

        // Ciezsza ryba = wezsza strefa trafienia i szybszy wskaznik (trudniejsze celowanie)

        float szerokoscStrefy = AktualnaSzerokoscStrefyCelowania();

        float maxX = PoziomTorX + PoziomTorSzerokosc - szerokoscStrefy;

        _celStrefaX = Raylib.GetRandomValue(PoziomTorX, (int)maxX);

        _celWskaznikX = PoziomTorX;

        _celWskaznikPredkoscAktualna = AktualnaPredkoscWskaznikaCelowania();

        _celBylWPrawo = false;

    }



    /// <summary>

    /// Aktualizuje mini-gre #2: wskaznik jedzie w lewo/prawo (1 pelny przebieg),

    /// a SPACJA zatrzymuje go - trafienie zbliza rybe, pudlo pozwala jej uciec.

    /// </summary>

    private void AktualizujCelowanie(float dt)

    {

        // Strzal - SPACJA zatrzymuje wskaznik w biezacym miejscu

        if (Raylib.IsKeyPressed(KeyboardKey.Space))

        {

            float srodekWskaznika = _celWskaznikX + PoziomWskaznikRozmiar / 2f;

            bool trafienie = srodekWskaznika >= _celStrefaX

                             && srodekWskaznika <= _celStrefaX + AktualnaSzerokoscStrefyCelowania();

            if (trafienie)

            {

                // Dzwiek sukcesu przy trafieniu w zielona strefe

                Raylib.PlaySound(_dzwiekTrafienie);



                // Trafienie: ryba zbliza sie o 5-10 m

                _odleglosc -= Raylib.GetRandomValue(5, 10);

                if (_odleglosc <= 0f)

                {

                    _odleglosc = 0f;

                    ZlapRybe();

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



        // Ruch wskaznika w lewo i w prawo (ping-pong)

        _celWskaznikX += _celWskaznikPredkoscAktualna * dt;



        float lewa = PoziomTorX;

        float prawa = PoziomTorX + PoziomTorSzerokosc - PoziomWskaznikRozmiar;

        if (_celWskaznikX >= prawa)

        {

            _celWskaznikX = prawa;

            _celWskaznikPredkoscAktualna = -MathF.Abs(_celWskaznikPredkoscAktualna); // zawroc w lewo

            _celBylWPrawo = true;

        }

        else if (_celWskaznikX <= lewa)

        {

            _celWskaznikX = lewa;

            _celWskaznikPredkoscAktualna = MathF.Abs(_celWskaznikPredkoscAktualna); // zawroc w prawo



            // Pelny przebieg (w prawo i z powrotem) bez strzalu - automatyczne pudlo

            if (_celBylWPrawo)

            {

                WykonajPudlo();

            }

        }

    }



    /// <summary>

    /// Rozstrzyga pudlo: ryba zawsze ucieka o 20-50 m, a z 50% szans zerwie linke (GAME OVER).

    /// </summary>

    private void WykonajPudlo()

    {

        // Dzwiek bledu przy pudle

        Raylib.PlaySound(_dzwiekPudlo);



        _odleglosc += Raylib.GetRandomValue(AktualnyPudloOdrzutMin(), AktualnyPudloOdrzutMax());

        if (_odleglosc > MaksymalnaOdleglosc)

        {

            _odleglosc = MaksymalnaOdleglosc;

        }



        // 50% szans, e ryba sie spina i zrywa linke

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

    /// Zwraca wspolczynnik trudnosci wynikajacy z masy ryby: 0 dla 1 kg, 1 dla 40 kg.

    /// Ciezsza ryba = trudniejszy poziom.

    /// </summary>

    

    /// <summary>
    /// Losuje mase ryby w zakresie 1-40 kg tak, by wieksze ryby trafialy sie
    /// tym rzadziej, im sa ciezsze (krzywa wykladniczo-malejaca wag 1/masa^p).
    /// p=StalaRzadkosciKg steruje stromoscia: male ryby dominuja, blisko 40 kg = rzadkie trofeum.
    /// </summary>
    private float LosujMaseRyby()
    {
        // Krzywa rzadkosci: waga ~ 1/masa^StalaRzadkosciKg -> duze ryby rzadziej.
        int n = MasaMaxKg - MasaMinKg + 1; // 40
        double[] wagi = new double[n];
        double suma = 0.0;
        for (int m = 0; m < n; m++)
        {
            double masa = m + 1; // 1..40 kg
            double w = 1.0 / System.Math.Pow(masa, (double)StalaRzadkosciKg);
            wagi[m] = w;
            suma += w;
        }

        // 1) wybierz pelny kilogram (1..40) tej sama krzywa
        double r = Raylib.GetRandomValue(1, 100000) / 100000.0 * suma;
        double acc = 0.0;
        int wybranyKg = MasaMinKg;
        for (int m = 0; m < n; m++)
        {
            acc += wagi[m];
            if (r <= acc)
            {
                wybranyKg = m + 1; // 1..40
                break;
            }
        }

        // 2) dodaj losowy ulamek do 3 miejsc po przecinku (0..999/1000)
        //    dla MasaMaxKg nie przekraczamy gornej granicy (ulamek = 0)
        float ulamek = (wybranyKg >= MasaMaxKg)
            ? 0f
            : Raylib.GetRandomValue(0, 999) / 1000f;

        float waga = wybranyKg + ulamek;
        // zaokraglenie do 3 miejsc po przecinku
        waga = (float)(System.Math.Round(waga, 3));
        return waga;
    }

private float WspolczynnikTrudnosci()

    {

        return (_masaRyby - MasaMinKg) / (float)(MasaMaxKg - MasaMinKg);

    }



    /// <summary>

    /// Biezaca predkosc zwijania linki (SPACJA trzymana): im ciezsza ryba, tym wolniejsze zwijanie.

    /// </summary>

    private float AktualnaSzybkoscZwijania()

    {

        return SzybkoscZwijania + (SzybkoscZwijaniaMin - SzybkoscZwijania) * WspolczynnikTrudnosci();

    }



    /// <summary>

    /// Biezaca predkosc ucieczki ryby (SPACJA puszczona): im ciezsza ryba, tym szybsza ucieczka.

    /// </summary>

    private float AktualnaSzybkoscUcieczki()

    {

        return SzybkoscUcieczki + (SzybkoscUcieczkiMax - SzybkoscUcieczki) * WspolczynnikTrudnosci();

    }



    /// <summary>

    /// Biezaca predkosc dryfu wskaznika wzgledem zielonej strefy:

    /// im ciezsza ryba, tym szybciej wskaznik plynie (trudniej utrzymac go w strefie).

    /// </summary>

    private float AktualnaPredkoscWskaznika()

    {

        return WskaznikPredkosc + (WskaznikPredkoscMax - WskaznikPredkosc) * WspolczynnikTrudnosci();

    }



    /// <summary>

    /// Biezaca szerokosc zielonej strefy trafienia w mini-grze #2 (celowanie):

    /// im ciezsza ryba, tym wezsza strefa (trudniej trafic).

    /// </summary>

    private float AktualnaSzerokoscStrefyCelowania()

    {

        return PoziomStrefaSzerokosc + (SzerokoscStrefyMin - PoziomStrefaSzerokosc) * WspolczynnikTrudnosci();

    }



    /// <summary>

    /// Biezaca predkosc wskaznika w mini-grze #2 (celowanie):

    /// im ciezsza ryba, tym szybszy wskaznik ping-pong (trudniej trafic).

    /// </summary>

    private float AktualnaPredkoscWskaznikaCelowania()

    {

        return PoziomWskaznikPredkosc + (PoziomWskaznikPredkoscMax - PoziomWskaznikPredkosc) * WspolczynnikTrudnosci();

    }



    /// <summary>

    /// Dolna granica losowego odrzutu przy pudle: im ciezsza ryba, tym dalej ucieka po pudle.

    /// </summary>

    private int AktualnyPudloOdrzutMin()

    {

        return (int)(PudloOdrzutMin + (PudloOdrzutCiezkiejMin - PudloOdrzutMin) * WspolczynnikTrudnosci());

    }



    /// <summary>

    /// Gorna granica losowego odrzutu przy pudle: im ciezsza ryba, tym dalej ucieka po pudle.

    /// </summary>

    private int AktualnyPudloOdrzutMax()

    {

        return (int)(PudloOdrzutMax + (PudloOdrzutCiezkiejMax - PudloOdrzutMax) * WspolczynnikTrudnosci());

    }



    /// <summary>

    /// KoL"czy runde sukcesem (ryba zlapana) i aktualizuje rekord najwiekszej zlapanej ryby w tej sesji.

    /// </summary>

    private void ZlapRybe()

    {

        _stan = Stan.Zlapana;

        if (_masaRyby > _rekordKg)

        {

            _rekordKg = _masaRyby;

            _czyNowyRekord = true;

        }

    }



    /// <summary>

    /// <summary>
    /// Formatuje mase (kg) do napisu z 3 miejscami po przecinku i przecinkiem
    /// dziesietnym niezaleznie od ustawien regionu (np. 4,432).
    /// </summary>
    private static string WagaZPrzecinkiem(float kg)
    {
        string s = kg.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        return s.Replace('.', ',');
    }

    /// <summary>

    /// Rysuje cala scene: tor, zielona strefe, wskaznik, wynik oraz ekran koL"ca gry.

    /// </summary>

    private void Rysuj()

    {

        Raylib.BeginDrawing();

        Raylib.ClearBackground(KolorTla);



        // Szary tor (pionowy pasek) z obrysem

        Raylib.DrawRectangle(TorX, TorY, TorSzerokosc, TorWysokosc, KolorTor);

        Raylib.DrawRectangleLines(TorX, TorY, TorSzerokosc, TorWysokosc, KolorTorKrawedz);



        // Zolta strefa ryzyka: pas powyzej i ponizej zielonej, tego samego X

        float strefaX = TorX + (TorSzerokosc - StrefaSzerokosc) / 2f;

        int zYg = (int)(_strefaY - StrefaRyzykoWysokosc);

        int zYd = (int)(_strefaY + StrefaWysokosc);

        Raylib.DrawRectangle((int)strefaX, zYg, (int)StrefaSzerokosc, (int)StrefaRyzykoWysokosc, KolorStrefaRyzyko);

        Raylib.DrawRectangle((int)strefaX, zYd, (int)StrefaSzerokosc, (int)StrefaRyzykoWysokosc, KolorStrefaRyzyko);

        // Zielona strefa (wysrodkowana poziomo, na pb po zoltych pasach)

        Raylib.DrawRectangle((int)strefaX, (int)_strefaY, (int)StrefaSzerokosc, (int)StrefaWysokosc, KolorStrefa);



        // Czerwony wskaznik - niezalezna pozycja w szarym torze

        float wskaznikX = TorX + (TorSzerokosc - WskaznikRozmiar) / 2f;

        float wskaznikY = _wskaznikY;

        Raylib.DrawRectangle((int)wskaznikX, (int)wskaznikY, (int)WskaznikRozmiar, (int)WskaznikRozmiar, KolorWskaznik);



        // PodpowiedLs sterowania (lewy gorny rog)

        Raylib.DrawText("SPACJA - w gore | puszczenie - w dol", 30, 10, 30, KolorTekst);

        // HUD: rekord najwiekszej zlapanej ryby w tej sesji (prawy gorny rog)

        string tekstRekordu = $"Rekord: {WagaZPrzecinkiem(_rekordKg)} kg";

        int xRekord = SzerokoscOkna - 20 - Raylib.MeasureText(tekstRekordu, 24);

        Raylib.DrawText(tekstRekordu, xRekord, 45, 24, KolorRybaCialo);



        RysujPasekOdleglosci();



        // Mini-gra #2 - poziome celowanie (nakladka na zamrozona gre #1)

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

            Raylib.DrawRectangle((int)_celStrefaX, PoziomTorY, (int)AktualnaSzerokoscStrefyCelowania(), PoziomTorWysokosc, KolorStrefa);



            // Czerwony wskaznik jadacy w lewo/prawo

            int wskY = PoziomTorY + (PoziomTorWysokosc - (int)PoziomWskaznikRozmiar) / 2;

            Raylib.DrawRectangle((int)_celWskaznikX, wskY, (int)PoziomWskaznikRozmiar, (int)PoziomWskaznikRozmiar, KolorWskaznik);

        }



        // Ekran koL"ca gry

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

            Raylib.DrawText(wygrana, xWy, 180, rozmiarWygrana, KolorStrefa);



            // Masa zlowionej ryby (im ciezsza, tym trudniejszy byl poziom)

            string tekstMasyZlapanej = $"Zlapales rybe o masie {WagaZPrzecinkiem(_masaRyby)} kg!";

            int xMasa = (SzerokoscOkna - Raylib.MeasureText(tekstMasyZlapanej, 28)) / 2;

            Raylib.DrawText(tekstMasyZlapanej, xMasa, 260, 28, KolorRybaCialo);



            // Komunikat o pobiciu rekordu sesji

            if (_czyNowyRekord)

            {

                const int rozmiarRekord = 40;

                string nowyRekord = "NOWY REKORD!";

                int xNr = (SzerokoscOkna - Raylib.MeasureText(nowyRekord, rozmiarRekord)) / 2;

                Raylib.DrawText(nowyRekord, xNr, 305, rozmiarRekord, KolorWskaznik);

            }



            string podpowiedz = "Nacisnij ENTER, aby zagrac ponownie";

            int xHint = (SzerokoscOkna - Raylib.MeasureText(podpowiedz, 26)) / 2;

            Raylib.DrawText(podpowiedz, xHint, 380, 26, KolorTekst);

        }



        Raylib.EndDrawing();

    }



    /// <summary>

    /// Rysuje na dole ekranu poziomy pasek odleglosci z ikona ryby.

    /// Lewy koniec paska = 0 m (ryba przy graczu), prawy = PasekMetryMax.

    /// Im mniejsza odleglosc do ryby, tym rybka bliej lewej strony.

    /// </summary>

    private void RysujPasekOdleglosci()

    {

        // Proporcja pozostalej odleglosci (0..1): 1 = daleko (250 m), 0 = zlapana

        float ratio = Math.Clamp(_odleglosc / PasekMetryMax, 0f, 1f);



        // Licznik odleglosci nad paskiem (wysrodkowany nad skala z rybka)

        string tekstOdleglosci = $"Odleglosc: {(int)_odleglosc} m";

        int xOdleglosc = (PasekOdleglosciX + PasekOdleglosciSzerokosc / 2) - Raylib.MeasureText(tekstOdleglosci, 24) / 2;

        Raylib.DrawText(tekstOdleglosci, xOdleglosc, PasekOdleglosciY - 46, 24, KolorTekst);



        // Tlo paska

        Raylib.DrawRectangle(PasekOdleglosciX, PasekOdleglosciY,

            PasekOdleglosciSzerokosc, PasekOdleglosciWysokosc, KolorPasekTlo);



        // Pozycja rybki: 0 m - lewy koniec, 250 m - prawy koniec

        int fishX = PasekOdleglosciX + (int)(ratio * PasekOdleglosciSzerokosc);

        int centerY = PasekOdleglosciY + PasekOdleglosciWysokosc / 2;

        int bob = (int)(MathF.Sin((float)Raylib.GetTime() * 5f) * 3f);

        centerY += bob;



        // Ogon (trAljkat po prawej stronie ciala)

        Raylib.DrawTriangle(

            new Vector2(fishX + 10, centerY),

            new Vector2(fishX + 20, centerY - 9),

            new Vector2(fishX + 20, centerY + 9),

            KolorRybaOgon);



        // Cialo (elipsa)

        Raylib.DrawEllipse(fishX, centerY, 14f, 9f, KolorRybaCialo);



        // Oko (po lewej stronie ciala - rybka "patrzy" w strone gracza)

        Raylib.DrawCircle(fishX - 7, centerY - 3, 2.5f, KolorRybaOko);



        // Podpisy skali nad paskiem

        Raylib.DrawText("0 m", PasekOdleglosciX, PasekOdleglosciY - 22, 16, KolorTekst);

        string etykietaMax = $"{(int)PasekMetryMax} m";

        int xEtykMax = PasekOdleglosciX + PasekOdleglosciSzerokosc + 6 - Raylib.MeasureText(etykietaMax, 16);

        Raylib.DrawText(etykietaMax, xEtykMax, PasekOdleglosciY - 22, 16, KolorTekst);

    }



    /// <summary>

    /// Generuje efekt o zadanej czestotliwosci i dlugosci, a nastepnie laduje go jako Sound z pamieci.

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

    /// Tworzy w pamieci plik WAV (PCM 16-bit, mono, 22050 Hz) z krAltkim tonem

    /// o szybkim zaniku obwiedni - brzmi jak suche, mechaniczne "cyk".

    /// </summary>

    private static byte[] UtworzWavTik(float czestotliwosc, float czasTiku)

    {

        int liczbaProbek = (int)(czasTiku * ProbkiNaSekunde);

        byte[] wav = new byte[44 + liczbaProbek * 2];



        // Naglowek WAV (44 bajty) - RIFF/WAVE, fmt PCM mono 16-bit

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

        wav[22] = 1; wav[23] = 0;                                    // mono = 1 kanal

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

        wav[34] = 16; wav[35] = 0;                                   // 16 bitow na probke

        wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';

        int rozmiarDanych = liczbaProbek * 2;

        wav[40] = (byte)(rozmiarDanych & 0xFF);

        wav[41] = (byte)((rozmiarDanych >> 8) & 0xFF);

        wav[42] = (byte)((rozmiarDanych >> 16) & 0xFF);

        wav[43] = (byte)((rozmiarDanych >> 24) & 0xFF);



        // Probki: sinus z szybkim zanikiem - suchy, mechaniczny "cyk"

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

