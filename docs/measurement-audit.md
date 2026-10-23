# Görbe burkolatszélek: mérési és területszámítási audit

Ellenőrzés dátuma: 2026-10-23.

## A javított modell

Az új normalizálás a képen megadott két burkolatszélre közös, metrikus burkolatsíkot
illeszt. Feltételei: sík útfelület, közel állandó burkolatszélesség, ismert tengely
menti szakaszhossz, egymásnak megfelelő kezdő és végkeresztmetszetek. A kamera
perspektíváját projektív leképezés kezeli; a lencsetorzítást ez nem korrigálja.

A `RoadPlaneModel` négy projektív paramétert illeszt csillapított legkisebb
négyzetes eljárással. A feltételek a két szél közötti állandó terepi távolság,
a szélek átlagos ívhosszából számított tengelyhossz és a végkeresztmetszet szélessége.
A két oldalon eltérő számú kijelölt pont is használható.

A rekonstruált síkon a keresztmetszetek párosítása a két szél átlagos érintőjének
normálisát követi. Ez elkerüli, hogy a belső oldali töröttvonal csúcsán több
szelvény ugyanarra a pontra essen. Az 513 keresztmetszet szelvényezése a
középpontjaik ívhosszán alapul. A kép és az útkoordináták között a metrikus síkon
keresztül történik az átszámítás, a keresztirányú perspektívát is figyelembe véve.

A fizikai hibaterületet a síkban rekonstruált folt négyszögeinek területösszege
adja. Ez bekerült mind a hibaterület-exportba, mind a technológiai kimutatás
területarányaiba. A teljes burkolat területe szolgál a részarány nevezőjeként;
a korábbi, szomszédos fénykép szélességével átlagolt összterület-szabály megmaradt.

## Független teszt és eredmények

A referenciaadatokat egy külön lyukkamera-vetítés állítja elő ismert terepi
pontokból. Nem a vizsgált leképezés inverzét használja.

- Valós szélesség: 4 m; tengely menti szakaszhossz: 10 m.
- Kamera: 2,5 m magasság, 0,25 radián bólintás, 1500 pixel fókusztávolság.
- A szakasz kezdete a kamera előtt 6 m; lencsetorzítás nincs.
- Bal szél: 13 pont; jobb szél: 9 pont. A simítást a program végzi.
- Vizsgált szelvények: 0,5–9,5 m, 0,5 m-es lépésekkel; keresztirányú
  koordináták a helyhibához: 0,5; 2; 3,5 m.
- A területteszt négy külön, 1 m széles foltot mér a 2–8 m szelvények között.
- A diagnosztika 0 és 0,15 radiános képi elfordítással is lefut.

A táblázat az elfordítás nélküli esetet mutatja. A maximumok a vizsgált pontokra
vonatkoznak, nem a teljes működési tartomány bizonyított hibakorlátai.

| Tengelyív sugara | Korábbi legnagyobb szelvényhiba | Új legnagyobb szelvényhiba | Új legnagyobb foltterület-hiba |
| --- | ---: | ---: | ---: |
| Egyenes | <0,001 m | <0,001 m | <0,001% |
| 100 m | 0,062 m | 0,014 m | 0,003% |
| 40 m | 0,355 m | 0,020 m | 0,007% |
| 20 m | 1,468 m | 0,027 m | 0,041% |
| 10 m | 3,467 m | 0,042 m | 0,116% |

A kiegészítő regressziós tesztek mindkét kanyarirányt, 0,12 radiános kameraelfordítást
és -0,15 radiános képi elfordítást is tartalmaznak. Ezekben a legnagyobb szelvényhiba
5,2 cm alatt maradt. Az S alakú, változó görbületű, állandó szélességű szakaszon
33 és 25 szélponttal a vizsgált pontok legnagyobb szelvényhibája 0,6 cm alatt volt.
Ezek sem terepi pontossági garanciák.

A 10 m sugarú ívben egy 1 m széles külső folt a 2–8 m szelvények között
analitikusan 6,9 m², a belső oldali párja 5,1 m². A korábbi szorzatos képlet mindkettőre
6 m²-t adott. A teszt az új területképletet az analitikus eredménnyel ellenőrzi,
és azt is vizsgálja, hogy az export a külső foltot egész m²-re kerekítve 7-ként írja ki.

Futtatás:

```powershell
dotnet run --project source/PaveMetric.GeometryTests -c Release
dotnet run --project source/PaveMetric.GeometryTests -c Release -- --measurement-audit
```

A normál regresszió elvárt hibahatárokat ellenőriz a konkrét tesztesetekre.
Az audit részletesen kiírja az eltéréseket, de lefutása nem általános tanúsítás.

## Ismert bázisból végzett szélességbeállítás

A bázist egy útkeresztmetszetben kell kijelölni; hosszát milliméterben kell megadni.
Az arányképlet `új W = bázis_m / (|korrigált X2 - X1| / jelenlegi W)`.
Mivel a metrikus sík illesztése maga is függ a szélességtől, a kalibrálás legfeljebb
nyolc lépésben újrailleszti a síkot a friss szélességgel. A végén ellenőrzi a
bázis keresztirányúságát. A tesztek 2 és 6 m-es kiinduló szélességet is ellenőriznek
a valós 4 m helyett, mindkét kanyarirányban.

A keresztirányúság kijelölési toleranciája `max(0,02 m; bázis_m * 0,05)` hosszirányú
eltérés. Ez nem pontossági állítás. Az azonos pontokat, nem véges vagy nem pozitív
bázishosszt, illetve jelentősen hosszirányú referenciát a kalibrálás elutasítja.

## Kompatibilitás és sikertelen illesztés

Az újonnan normalizált fényképek `RoadModelVersion=1` értéket kapnak a projektben.
A korábbi, verziómező nélküli fájlok továbbra is a régi modell szerint nyílnak meg,
így a korábban rögzített hibák nem értelmeződnek át automatikusan.
Az adott fénykép újranormalizálása kapcsolja be az új modellt, és automatikusan
átvetíti a rögzített hibákat. A régi modellből meghatározott képi körvonal megmarad;
az új koordinátahatárok ennek a befoglaló határai. A hiba nem alakul át nagyobb
téglalappá: a körvonal külön `ImageOutline` adatként mentődik a projektbe.
A perspektívakép, a felülnézet, a kijelölés, a szerkesztés és a képexport is ezt
használja. A terület az új burkolatsíkra vetített körvonal fizikai területe.

Az átvetítés minden hibára előre elkészül. Ha valamelyik körvonal nem értelmezhető
az új geometriával, a normalizálás magyarázó üzenettel megszakad, és sem a korábbi
geometria, sem a rögzített hibák nem módosulnak. Az új körvonalak képi visszavetítését
0,5 pixeles toleranciával ellenőrzi a program. Az ismételt normalizálás a már tárolt
képi körvonalból indul, így nem növeli újra és újra a befoglaló téglalapot.

A normalizálás visszavonható és újra végrehajtható. A korábbi hibaszerkesztési
előzmények a hozzájuk tartozó koordinátarendszerben maradnak használhatók.
A regressziós tesztek a körvonal pixelhelyének megőrzését, a fizikailag számított
területet, a mentést–visszatöltést, a mozgatást, a kijelölést, az ismételt
normalizálást és a teljes visszavonási sorrendet is ellenőrzik.

A valódi `farkasvolgy.ppr` projekttel végzett ellenőrzés 543 fényképet és 2188
hibabejegyzést sikeresen átvetített, majd külön fájlba mentett. A próbában a
szelvényhatáron legfeljebb kb. 14 cm-rel túlnyúló régi hibák és három nulla
területű bejegyzés is megmaradt. A metrikus modell a végső keresztmetszeteket
röviden meghosszabbítja, így a korábbi kis túlnyúlások nem szorulnak a határra.
Görbéknél ez helyi végérintő szerinti közelítés; egyenes széleknél a teljes
projektív leképezés folytatása. A kiterjesztés nem új terepi kalibráció.

Az 1+230 erősen balra ívelő szakaszán a régi egyenes kalibrációból megmaradt
hibák az új íves útszélen túl is helyezkednek el. A koordinátaváltás az új modell
mindkét ágában (illesztett sík és tartalék görbemodell) a keresztmetszetek teljes
egyenesét használja, és az egyes bilineáris sávokat analitikusan invertálja.
A görbe végén állandó szélességű végérintő folytatódik, így nem keletkezik távoli,
összeomló szélességű meghosszabbítás. A verzió nélküli projektek régi értelmezése
változatlan marad az újranormalizálásig.

A `farkasvolgy_atvetitett_1230_ellenorzott.ppr` ellenőrzési másolaton a Normalizál
gombbal is lefutott a művelet, és megjelent a felülnézet. A mentett 543 fénykép
2188 hibakörvonala pontról pontra azonos a teljesen átvetített bemenettel; csak
az 1+230 kalibrációja változott. A szakasz hat hibáján a legnagyobb képi
oda-vissza vetítési eltérés 5e-13 pixel alatt volt. Mentés, visszatöltés és
ismételt normalizálás után a területek változatlanok maradtak: kátyú 3,642654 m²,
hálós repedések összege 34,727900 m². Ennél a kézi szélezésnél a metrikus
illesztés nem fogadható el; ezek a tartalék görbemodell közelítő területei,
nem igazolt terepi mérési eredmények. A sárga figyelmeztetés megmarad.

A nem illeszthető szélekhez a korábbi közelítés marad elérhető. A Normalizálás gomb
figyelmeztető színt és magyarázó súgót kap. Ez a jelzés a fénykép betöltésekor is
megjelenik. A modellverzió és a területszámítás XML-mentés utáni visszaállítását,
a régi fájlok kezelését, a kereszteződő szélek elutasítását és a hibás bemeneteket
külön tesztek ellenőrzik.

Az illesztés elfogadási feltétele a súlyozott maradékok 2%-nál kisebb RMS-e és
8%-nál kisebb legnagyobb abszolút maradéka. Ez geometriai illeszkedési szűrő,
nem garantált mérési hibaszázalék. A módszer nem oldja meg a magasságban változó
út, a változó szélesség, a rosszul kijelölt keresztmetszetek vagy a lencsetorzítás
általános problémáját. Ismert terepi kontrollpontokkal végzett ellenőrzés továbbra
is szükséges, ha igazolt mérési pontosság a cél.
