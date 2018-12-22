# RoadsOfTheRim

A Rimworld mod allowing road construction on the world map

<hr>

## Introduction

I decided to try implementing something similar to Jecrell's amazing [RimRoads](https://github.com/jecrell/RimRoads) mod since - as of this writing - it has not been ported to Rimworld 1.0.

## Disclaimer

This is not only my first mod, but the first time for me to code in C#. On top of that, I am using Mono on Linux which is not the most widely used environment for coding Rimworld mods.

A lot of patience (on your side and mine) will probably be necessary for this to work !

## How this works

Roads of the Rim introduces the concept of road construction sites.

A road construction site can be created by a Caravan on the world map. At the time of creation, one neighbouring tile must be picked to decide where to build the road to. The player must also pick a type of road :

- Dirt path
- Dirt road
- Stone road
- Asphalt road

Once a construction site is created, it will appear on the map and any caravan on site can use the __Work on road__ command. Work will accumulate until eventually the road is built and the construction site disappears.

Since building a road will require a lot of time and resources, the mod currently only supports building from one tile to a neighbouring one. The operation can then be repeated to link multiple tiles.

It is possible to build a road to a tile that would otherwise be impassable, as long as it's not water

## Construction costs

Each road type has a base cost in wood, stone, steel and even chemfuel (representing explosives) as well as an amount of work needed.

The construction cost is then further modified by the terrain of the construction site, as well as the terrain of the target neighbouring tile. The total cost required for each ressource is calculated when the site is created & shown in the info pane.

Terrain increases the cost of all resources & work needed as follows :

Hillinness will increase the cost linearly from 0 to 100%

Swampiness will increase the cost linearly from 0 to 200 %

Altitude will not increase the cost until above a certain number (default 1000m), then linearly by 100% every 2000 metres.

River crossing requires building a bridge, which adds an additional cost on top of the total. The cost of the bridge is also increased by the above effects.

All resources must come from the caravan working on the site. If it runs outs of any resource, the work will immediately stop.

## Caravan work

The total amount of work a caravan can provide for every time unit (or "ticks") depends on pawns with the build ability & the number of pack animals present. However, animals can only make pawns up to twice as fast. Above that limit, more animals will not make a difference.

## Movement cost

In Rimworld, there are no differences between roads in terms of movement cost multiplier. They all simply make movement twice as fast (cost is 50%).

In Roads of the Rim, this has been tweaked and will impact not only roads newly built, _but also existing roads_. Paradoxically, this makes existing dirt paths and dirt roads less attractive when the mod is installed. Stone roads wil be as good and asphalt roads & highways a little better as per the table below (the lower, the faster).

|Type 	|Rimworld standard |	Roads of the Rim
|-----|----|---|
Dirt path|50%|75%
Dirt road|50%|60%
Stone road|50%|50%
Asphalt road|50%|40%
Ancient asphalt road|50%|40%
Ancient asphalt highway|50%|40%

Note : All roads movement cost multipliers can be reverted back to their flat 50% multiplier in the settings.

## Roadmap (almost no pun intended)

- Proof of concept : On the world map, get a simple action to place a dirt path section, confirm it survives saving & loading. **DONE**
- Add a caravan job to build dirt path **DONE**
- Add types of roads : dirt road, stone road, asphalt road **DONE**
- Add material & work costs **DONE**
- Add terrain construction cost modifiers **DONE**
- Add bridge construction cost modifiers
- Add a com console menu to ask friendly factions to help on the construction of a road
- optional / distant future : add railroads & trains

## Acknowledgments

First and foremost, hats off to Jecrell for his mod RimRoads that until 1.0 did what Roads of the Rim aims to do, and whose tutorial How to Make a RimWorld Mod, Step by Step got me started.

Great tutorials from roxxploxx. A big thank you to Albion & Mehni for their help on the Ludeon forum. And they don't know it, but their code helped me a lot : Syrchalis, Ratysz & Skullywag !


