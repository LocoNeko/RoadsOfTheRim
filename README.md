<h1>RoadsOfTheRim</h1>
<p>A Rimworld mod allowing road construction on the world map</p>
<p><a href="https://github.com/LocoNeko/RoadsOfTheRim"><img src="https://camo.githubusercontent.com/ab9103a6b5968f79a67ff0a0a09220d0597e3199/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f72656c656173652f4c6f636f4e656b6f2f526f6164734f6654686552696d2f616c6c2e737667" alt="" data-canonical-src="https://img.shields.io/github/release/LocoNeko/RoadsOfTheRim/all.svg" style="max-width:100%;"></a></p>
<hr>
<h2><a id="user-content-introduction" class="anchor" aria-hidden="true" href="#introduction"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Introduction</h2>
<p>I decided to try implementing something similar to Jecrell's amazing <a href="https://github.com/jecrell/RimRoads">RimRoads</a> mod since - as of this writing - it has not been ported to Rimworld 1.0.</p>
<h2><a id="user-content-disclaimer" class="anchor" aria-hidden="true" href="#disclaimer"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Disclaimer</h2>
<p>This is not only my first mod, but the first time for me to code in C#. On top of that, I am using Mono on Linux which is not the most widely used environment for coding Rimworld mods.</p>
<p>A lot of patience (on your side and mine) will probably be necessary for this to work !</p>
<h2><a id="user-content-how-this-works" class="anchor" aria-hidden="true" href="#how-this-works"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>How this works</h2>
<p>Roads of the Rim introduces the concept of <em>road construction sites</em>.</p>
<p>A road construction site can be created by a Caravan on the world map. At the time of creation, one neighbouring tile must be picked to decide where to build the road to. The player must also pick a type of road :</p>
<ul>
<li>Dirt path</li>
<li>Dirt road</li>
<li>Stone road</li>
<li>Asphalt road</li>
</ul>
<p>Once a construction site is created, it will appear on the map and any caravan on site can use the <em><strong>Work on road</strong></em> command. Work will accumulate until eventually the road is built and the construction site disappears.</p>
<p>Since building a road will require a lot of time and resources, the mod currently only supports building from one tile to a neighbouring one. The operation can then be repeated to link multiple tiles.</p>
<p>It is possible to build a road to a tile that would otherwise be impassable, as long as it's not water</p>
<h2><a id="user-content-construction-costs" class="anchor" aria-hidden="true" href="#construction-costs"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Construction costs</h2>
<p>Each road type has a base cost in wood, stone, steel and even chemfuel (representing explosives) as well as an amount of work needed.</p>
<p>The construction cost is then further modified by the terrain of the construction site, as well as the terrain of the target neighbouring tile. The total cost required for each ressource is calculated when the site is created &amp; shown in the info pane.</p>
<p>Terrain increases the cost of all resources &amp; work needed as follows :</p>
<p>Hillinness will increase the cost linearly from 0 to 100%</p>
<p>Swampiness will increase the cost linearly from 0 to 200 %</p>
<p>Altitude will not increase the cost until above a certain number (default 1000m), then linearly by 100% every 2000 metres.</p>
<p>River crossing requires building a bridge, which adds an additional cost on top of the total. The cost of the bridge is also increased by the above effects.</p>
<p>All resources must come from the caravan working on the site. If it runs outs of any resource, the work will immediately stop.</p>
<h2><a id="user-content-caravan-work" class="anchor" aria-hidden="true" href="#caravan-work"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Caravan work</h2>
<p>The total amount of work a caravan can provide for every time unit (or "ticks") depends on pawns with the build ability &amp; the number of pack animals present. However, animals can only make pawns up to twice as fast. Above that limit, more animals will not make a difference.</p>
<h2><a id="user-content-movement-cost" class="anchor" aria-hidden="true" href="#movement-cost"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Movement cost</h2>
<p>In Rimworld, there are no differences between roads in terms of movement cost multiplier. They all simply make movement twice as fast (cost is 50%).</p>
<p>In Roads of the Rim, this has been tweaked and will impact not only roads newly built, <em><strong>but also existing roads</strong></em>. Paradoxically, this makes existing dirt paths and dirt roads less attractive when the mod is installed. Stone roads wil be as good and asphalt roads &amp; highways a little better as per the table below (the lower, the faster).</p>
<table>
<thead>
<tr>
<th>Type</th>
<th>Rimworld standard</th>
<th>Roads of the Rim</th>
</tr>
</thead>
<tbody>
<tr>
<td>Dirt path</td>
<td>50%</td>
<td>75%</td>
</tr>
<tr>
<td>Dirt road</td>
<td>50%</td>
<td>60%</td>
</tr>
<tr>
<td>Stone road</td>
<td>50%</td>
<td>50%</td>
</tr>
<tr>
<td>Asphalt road</td>
<td>50%</td>
<td>40%</td>
</tr>
<tr>
<td>Ancient asphalt road</td>
<td>50%</td>
<td>40%</td>
</tr>
<tr>
<td>Ancient asphalt highway</td>
<td>50%</td>
<td>40%</td>
</tr>
</tbody>
</table>
<p><em>Note : All roads movement cost multipliers can be reverted back to their flat 50% multiplier in the settings.</em></p>
<h2><a id="user-content-roadmap-almost-no-pun-intended" class="anchor" aria-hidden="true" href="#roadmap-almost-no-pun-intended"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Roadmap (<em>almost no pun intended</em>)</h2>
<ul>
<li>Proof of concept : On the world map, get a simple action to place a dirt path section, confirm it survives saving &amp; loading. <strong>DONE</strong></li>
<li>Add a caravan job to build dirt path <strong>DONE</strong></li>
<li>Add types of roads : dirt road, stone road, asphalt road <strong>DONE</strong></li>
<li>Add material &amp; work costs <strong>DONE</strong></li>
<li>Add terrain construction cost modifiers <strong>DONE</strong></li>
<li>Add bridge construction cost modifiers</li>
<li>Add a com console menu to ask friendly factions to help on the construction of a road</li>
<li><em>optional</em> / <em>distant future</em> : add railroads &amp; trains</li>
</ul>
<h2><a id="user-content-acknowledgments" class="anchor" aria-hidden="true" href="#acknowledgments"><svg class="octicon octicon-link" viewBox="0 0 16 16" version="1.1" width="16" height="16" aria-hidden="true"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg></a>Acknowledgments</h2>
<p>First and foremost, hats off to Jecrell for his mod <a href="https://github.com/jecrell/RimRoads">RimRoads</a> that until 1.0 did what Roads of the Rim aims to do, and whose tutorial <em><a href="https://ludeon.com/forums/index.php?topic=33219" rel="nofollow">How to Make a RimWorld Mod, Step by Step</a></em> got me started.</p>
<p>Great tutorials from roxxploxx. A big thank you to Albion &amp; Mehni for their help on the Ludeon forum. And they don't know it, but their code helped me a lot : Syrchalis, Ratysz &amp; Skullywag !</p>
<hr>
<p><a href="mailto:loconeko73@gmail.com"><img src="https://camo.githubusercontent.com/e5dbcbd24b7036dc8aba9d8a15c0a25c9e5ab070/68747470733a2f2f696d672e736869656c64732e696f2f62616467652f656d61696c2d6c6f636f6e656b6f3733253430676d61696c2e636f6d2d626c75652e737667" alt="" data-canonical-src="https://img.shields.io/badge/email-loconeko73%40gmail.com-blue.svg" style="max-width:100%;"></a>
<a href="https://opensource.org/licenses/MIT" rel="nofollow"><img src="https://camo.githubusercontent.com/6f2b1bcf77c5ae904465c4bd746dd8a817c8c298/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f6c6963656e73652f4c6f636f4e656b6f2f526f6164734f6654686552696d2f616c6c2e737667" alt="" data-canonical-src="https://img.shields.io/github/license/LocoNeko/RoadsOfTheRim/all.svg" style="max-width:100%;"></a></p>
