# Sprint 0 - Game Design Document : Tower Defense
Naam: kareem

Klas: sd2a

Datum: 9/14/2026

build is in https://github.com/karlaps45/tower-defence-mania/tree/main/maniaman


## 1. Titel en elevator pitch
Titel: customazia


in de game maak je jouw eigen character sterker door upgrades/loot van enemies.
hierdoor maak je je eigen towers.
## 2. Wat maakt jouw tower defense uniek

customization van je eigen towers.

## 3. Schets van je level en UI


Je schets bevat minimaal:
1. Het pad waar de vijanden over lopen met beginpunt en eindpunt.
2. De plaatsen waar torens gebouwd kunnen worden.
3. De locatie van de basis of goal die verdedigd moet worden.
4. De UI onderdelen geld, wave teller, levens, startknop en pauzeknop.
5. Een legenda met symbolen of kleuren voor torens, vijanden, pad, basis en UI.

## 4. Torens
er is aan het begin maar 1 toren in de game, die je zelf upgrade met enemy equipment.
slow firing rate, low damage and medium range.
equipment is procedurally generated, dus uiteindelijk zijn alle towers anders.


## 5. Vijanden
Vijand 1: een normale enemy die je aanvalt.
vijand 2: een tank enemy die langzaam beweegt maar veel hp heeft
vijand 3: een speed focused enemy die je towers snel aanvalt, en snel beweegt, maar heeft low hp

## 6. Gameplay loop

1. start de ronde

2. vecht enemies

3. heal je characters

4. upgrade characters

5. ga verder

## 7. Progressie
progressie gaat via equipment, waarmee je je eigen unieke towers maakt.
Bijv,
een tower krijgt een helmet/charm die hem laat exploderen met bullets, en een die zijn bullets intot vuur veranderd

een healing tower krijgt een charm om zijn shots te veranderen naar een shotgun spread, en een chestplate om sneller aan te vallen, dan heb je een grootere AOE heal.



## 8. Risico’s en oplossingen volgens PIO
- Probleem 1: te groot om te maken
- Impact: project komt niet af
- Oplossing: in mijn trello checken wat MOET en wat NIEt

- Probleem 2: skill tree is te complex 
- Impact: helpt met je towers apart van elkaar maken. veel verdieping en unieke playthroughs voor de speler.
- Oplossing: de skill tree eruit halen.


- Probleem 3: abilities zijn te groot om te maken
- Impact: abilities zijn het belangerijkste ding in de game dat het uniek maakt.
- Oplossing: maak abilities minder complex
  
## 9. Planning per sprint en mechanics
Schrijf per sprint welke mechanics jij oplevert in de build. Denk aan voorbeelden zoals vijandbeweging over een pad, torens plaatsen, doel kiezen en schieten, waves starten, UI voor geld en levens, upgrades, jouw unieke feature.

Sprint 1 mechanics: basic tower defence klaar (enemies kunnen ook schieten)

Sprint 2 mechanics: spawning klaar

Sprint 3 mechanics: tower damageing, tower healing, tower customization klaar

Sprint 4 mechanics: equipment holpelijk

Sprint 5 mechanics: laatste touchups


## 10. Inspiratie
ik ben het meest geinspireerd door meerdere roguelikes.

## 11. Technisch ontwerp mini


### 11.1 Vijandbeweging over het pad
- Keuze:  de veiand gaat van waypoint naar waypoint, de waypoints zijn ontzichtbaar
- Risico: de vijand blijft vast op een waypoint
- Oplossing: stop alle waypoints in een lijst en ga van waypoint 1 naar 2 als hij dichtbij het waypoint is
- Acceptatie: de vijand gaat direct door


### 11.2 Doel kiezen en schieten
- Keuze: de vijanden, en de spelers kunnen elkaar schieten als ze dichtbij zijn
- Risico: de spelers en vijanden vallen verkeerde plekken aan, of elkaar aan.
- Oplossing: zorg evoor dat de spelers en vijanden elkaar kunnen verschillen
- Acceptatie: de towers, en vijanden vallen elkaar aan.

### 11.3 Waves en spawnen
- Keuze: de enemies spawnen in zodat je ze kan schieten
- Risico: waves spawnen oneindig in
- Oplossing: zorg ervoor dat er een limiet aan de enemiez zitten
- Acceptatie: de enemies stoppen na een paar enemies

  
### 11.4 Economie en levens
- Keuze: de speler koopt dezelvde torens, meer equipment kopen en dit nummer, en het nummer van levens kan je zien op je scherm
- Risico: de speler kan te makkelijk torens kopen en het geld kan er niet vanaf gaan
- Oplossing: ervoor zorgen dat health en coins op het ui worden laten zien
- Acceptatie: de health en coins gaan er vanaf het moment dat het er vanaf moet

### 11.5 UI basis
- Keuze: er moeten meerdere schermen zijn voor meerdere functies
- Risico: de schermen laten zichzelf niet zien als je er op klikt, of laten zichzelf allemaal tegelijkertijd zien
- Oplossing zorg ervoor dat alle objects op de juiste momenten zichzelf laten zien en dat ze niet allemaal actief zijn
- Acceptatie: het ui ziet er clean en goed uit
