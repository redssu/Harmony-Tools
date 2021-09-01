# Harmony-Tools

Zestaw narzędzi, które umożliwiają przetłumaczenie gry Danganronpa V3: Killing Harmony. Obecnie, wspierana jest tylko wersja na PC.

Wszystkie narzędzia są nieinteraktywne i mogą być używane za pomocą twojego ulubionego terminala, na przykład: Konsole, CMD lub PowerShell

Znajdziesz tutaj też jedno narzędzie "Explorer-Extension", które po uruchomieniu dodaje nową listę do menu kontekstowego, zawierającą skróty do reszty narzędzi, przez co można używać ich szybciej i łatwiej. Explorer-Extension wspiera obecnie tylko system Windows.

## Instalacja

W każdej wypuszczonej wersji będę umieszczał archiwum ZIP z programem ```Installer.exe``` i katalogiem ```bin``` w środku. Po uruchomieniu instalator skopiuje wszystkie pliki z katalogu ```bin``` do katalogu ```%ProgramFiles%\HarmonyTools``` i doda również tę ścieżkę do Zmiennych Środowiskowych, po to, by można było używać tych narzędzi w każdym katalogu za pomocą terminala. Zauważ, że wszystkie narzędzia w katalogu ```bin``` mają przedrostek "HT" - dodałem to w razie gdyby powstały konflikty nazw z innymi programami, a więc używając na przykład narzędzia ```Stx``` powinno wpisywać się ```HTStx```.

**Uwaga**: Instalator potrzebuje uprawnień administratora.

Po instalacji zaleca się zarejestrowanie nowego Menu Kontekstowego za pomocą Explorer-Extension. 


## Contributing

Wszelkie Pull Requesty są mile widziane. Dopiero zaczynam pisanie programów w C#, więc w kodzie na pewno znajdą się rzeczy, które można poprawić.


## Notki

Większość tych narzędzi to po prostu nakładki na narzędzia stworzone przez [CapitanSwag](https://github.com/jpmac26) - moje narzędzia nie powstałyby, gdyby nie jego ciężka praca. Moim celem natomiast było stworzenie zestawu narzędzi, które będą przyjazne w obsłudze dla tłumaczy oraz które nie będą wymagały wpisywania komend, więc przerobiłem kod [DRV3-Sharp](https://github.com/jpmac26/DRV3-Sharp) w ten sposób, by usunąć z nich interaktywność.

Repozytorium stworzone przez [EDDxample](https://github.com/EDDxample) ([Ultimate-DRv3-Toolset](https://github.com/EDDxample/ultimate-drv3-toolset)) było dla mnie kopalnią wiedzy, jeśli chodzi o pliki z czcionkami, bez nich nie napisałbym programu pakującego czcionki.


## Użycie

### EXPLORER-EXTENSION

Użycie:

```ExplorerExtension (--register | --unregister) [--lang=(EN | PL)] [--delete-original]```

Jeżeli parametr ```--register``` jest ustawiony, narzędzie utworzy nowe wpisy w Rejestrze Systemowym, co poskutkuje dodaniem do Eksploratora nowego Menu Kontekstowego, które będzie dostępne po prawym kliknięciu na plik lub katalog.

Jeżeli parametr ```--delete-original``` jest ustawiony podczas operacji rejestrowania, każde narzędzie uruchamiane za pomocą Menu Kontekstowego będzie uruchamiane z parametrem ```--delete-original```.

Parametr ```--lang``` jest po to, by można było zmieniać między (obecnie) dwoma dostępnymi językami. Domyślny to Angielski (EN). Ustawienie te dotyczy tylko tekstów w Menu Kontekstowym.

Jeżeli parametr ```--unregister``` jest ustawiony, program usunie wszystkie poprzednio dodane wpisy w Rejestrze Systemowym.

**Uwaga**: Ten program zakłada, że narzędzia zostały zainstalowane z dołączonym instalatorem - używa narzędzi, które znajdują się w katalogu ```%ProgramFiles%\HarmonyTools``` i mają przedrostek "HT".



### STX

Pliki STX zawierają głównie dialogi postaci, bądź zawierają w sobie czcionki.

Te narzędzie rozpakowuje plik ".STX" do formatu ".TXT", bądź pakuje plik ".TXT" do formatu ".STX". W celu zamiany czcionek, patrz sekcję narzędzia **FONT**.

Użycie:

```stx (--unpack|--pack) file_path [--delete-original] [--pause-after-error]```

Jeżeli parametr  ``` --delete-original``` jest ustawiony, oryginalny plik zostanie usunięty.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Jeżeli pakujesz plik ".TXT" do pliku ".STX" i jego nazwa kończy się na ".STX.TXT", narzędzie usunie tylko końcówkę ".TXT" z nazwy, by brzmiała jak oryginalny plik.


### DAT

Pliki DAT zawierają dane sformatowane w formie tabeli, zazwyczaj używane w minigrach lub rozprawach.

**Uwaga**: Niektóre pliki DAT nie zawierają danych w formie tabeli, przez co nie mogą być otwarte. Takie pliki głównie znajdują się w folderze "wrd_data".

Te narzędzie rozpakowuje plik ".DAT" do formatu ".CSV", bądź pakuje plik ".CSV" do formatu ".DAT". Pliki ".CSV" najłatwiej otworzyć programem Office Excel lub LibreOffice Calc.

Użycie:

```dat (--unpack|--pack) file_path [--delete-original] [--pause-after-error]```

Jeżeli parametr  ``` --delete-original``` jest ustawiony, oryginalny plik zostanie usunięty.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Jeżeli pakujesz plik ".CSV" do pliku ".DAT" i jego nazwa kończy się na ".DAT.CSV", narzędzie usunie tylko końcówkę ".CSV" z nazwy, by brzmiała jak oryginalny plik.


### WRD

Pliki WRD zawierają skrypty gry, z nich można wyczytać, kto mówi w danych liniach w pliku STX.

**Notka**: Pakowanie plików WRD nie jest obecnie możliwe.

Użycie:

```wrd --unpack file_path [--translate] [--delete-original] [--pause-after-error]```

Jeżeli parametr ```--translate``` jest ustawiony, nazwy kodów operacji są tłumaczone na bardziej zrozumiałe.

Jeżeli parametr  ```--delete-original``` jest ustawiony, oryginalny plik zostanie usunięty.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.


### SPC

Archiwa SPC są archiwami ogólnego przeznaczenia, które zawierają w sobie inne pliki, na przykład ".STX" lub ".SRD". Można je porównać do znanych Archiw ZIP.

Użycie:

```spc (--unpack|--pack) object_path [--delete-original] [--pause-after-error]```

```object_path``` powinien być ścieżką do katalogu, jeśli próbujesz stworzyć nowe archiwum SPC lub ścieżką do archiwum SPC przeznaczonego do rozpakowania.

Jeżeli parametr  ``` --delete-original``` jest ustawiony, oryginalny katalog lub archiwum zostaną usunięte.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Gdy rozpakowujesz archiwum SPC, narzędzie stworzy katalog z nazwą taką samą, jak archiwum, lecz z dopiskiem ".decompressed".

Gdy pakujesz katalog do postaci archiwum SPC, narzędzie analogicznie usunie dopisek ".decompressed". W ten sposób, po operacji rozpakowania i pakowania, otrzymasz tę samą nazwę archiwum SPC.

**Uwaga**: Nie usuwaj oraz nie modyfikuj pliku ```__spc_info.json```, który znajduje się w środku powstałego katalogu. Bez niego nie można zapakować katalogu do postaci archiwum SPC.



### SRD

Archiwa SRD przechowują głównie pliki związane z teksturami, bądź modelami gry. Narzędzie te pozwala tylko na wypakowywanie i zamianę tekstur.

Użycie:

```srd (--unpack|--pack) object_path [--delete-original] [--pause-after-error]```

```object_path``` powinien być ścieżką do katalogu zawierającego tekstury do podmiany w oryginalnym archiwum SRD lub ścieżką do archiwum SRD przeznaczonego do rozpakowania.

Jeżeli parametr  ``` --delete-original``` jest ustawiony, oryginalny katalog lub archiwum zostaną usunięte.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Gdy rozpakowujesz archiwum SRD, narzędzie stworzy katalog zawierający tekstury, o takiej samej nazwie co archiwum, lecz z dopiskiem ".decompressed"

Gdy pakujesz katalog do archiwum SRD, narzędzie zamieni tylko tekstury, które znajdują się w oryginalnym pliku SRD (jego kopia znajduje się w katalogu i ma nazwę ```_.srd```). Tak więc nie powinno zmieniać się nazw plików tekstur, by narzędzie mogło podmienić ich odpowiedniki w oryginalnym pliku. 

Z uwagi na to, że to narzędzie nie obsługuje zaawansowanej kompresji używanej przez twórców Danganronpa V3, nie powinno zamieniać się tekstur na marne, to jest; jeśli nic nie zmieniło się w pliku tekstury, usuń ją z katalogu, by narzędzie jej nie zamieniło bez powodu - w ten sposób archiwa SRD będą miały stosunkowo mały rozmiar. W moim przypadku, po zamienieniu wszystkich tekstur z menu głównego, rozmiar archiwum SRD urósł o ponad 400%. 

Po pakowaniu do archiwum SRD, narzędzie nazwie je tak jak katalog, z tą różnicą, że usunie z nazwy dopisek ".decompressed". 

**Uwaga**: Nie usuwaj oraz nie modyfikuj plików ```_.srd``` oraz (jeśli istnieje) ```_.srdi``` oraz (jeśli istnieje) ```_.srdv```. Bez nich nie można zapakować katalogu do postaci archiwum SRD.




### FONT

Użycie:

```font (--unpack|--pack) object_path [--gen-debug-image] [--pause-after-error]```

**PAKOWANIE CZCIONKI**

```object_path``` powinien być ścieżką do katalogu.

Jeżeli parametr ```--gen-debug-image``` jest ustawiony - narzędzie utworzy dodatkowy plik z dopiskiem ```__DEBUG_IMAGE```, który jest teksturą zawierającą wszystkie znaki po ich połączeniu w całość.

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Katalog, który zostanie zapakowany do pliku czcionki, musi zawierać w sobie plik ```__font_info.json```, który zawiera obiekt JSON z następującymi właściwościami:

- "FontName" - które zawiera nazwę użytej czcionki, w postaci tekstu. Na przykład: "ComicSans.otf". Jest to właściwość czysto informacyjna, ale jest wymagana.
- "Charset" - zawiera znaki, które znajdują się w czcionce. Przy pakowaniu czcionki możesz tu wpisać cokolwiek, skrypt wykryje znaki automatycznie.
- "ScaleFlag" - specjalna wartość z oryginalnego pliku czcionki, która wpływa na jej skalowanie w grze
- "Resources" - które zawiera listę zasobów. Lista powinna zawierać dwa ciągi znaków:
  - "font_table"
  - Oraz nazwę pliku tekstury. Można ją znaleźć wypakowując plik czcionki za pomocą narzędzia SRD (na przykład: "db_font00_US_win.bmp")

Plik `` __font_info.json`` jest automatycznie generowany po wypakowaniu pliku czcionek. Możesz użyć go jako przykład albo nawet zostawić taki, jaki jest oryginalnie.

W katalogu powinny znaleźć się pliki ".BMP", które będą służyć jako tekstury poszczególnych znaków. Pliki powinny być nazwane w postaci narastających numerów (na przykład: ```0000.bmp```, ```0001.bmp```,  i tak dalej). Numery te powinny być poprzedzone zerami w taki sposób, by każda nazwa miała stałą ilość znaków (więc, jeśli największy numer to ```127.bmp```, wszystkie jednocyfrowe nazwy powinny mieć przed sobą dwa zera, wszystkie dwucyfrowe nazwy powinny mieć przed sobą jedno zero). 

Każdy plik ".BMP" powinien mieć również plik ".JSON" o tej samej nazwie. Plik ".JSON" powinien zawierać taki obiekt:

``` js
{
  "Glyph": "S",   // <- Znak, który reprezentuje dana tekstura
  "Kerning": {    // <- Odstępy od znaku
    "Left": 2,
    "Right": 3,
    "Vertical": 4
  }
}
```



**ROZPAKOWYWANIE**

```object_path``` powinien być ścieżką do pliku ".SRD" lub ".STX"

Jeżeli parametr ```--pause-after-error``` jest ustawiony - narzędzie będzie czekać na interakcję użytkownika jeśli wystąpi jakiś błąd zanim zakończy działanie.

Narzędzie utworzy katalog o takiej samej nazwie, co plik wejściowy, dodając ".decompressed_font" na końcu jego nazwy.

Struktura katalogu będzie analogiczna do tej opisanej w sekcji **PAKOWANIE CZCIONKI**







