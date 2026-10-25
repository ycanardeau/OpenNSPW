using OpenNspw.Singleplayer;

namespace OpenNspw.Desktop;

// The single-player scenarios of NSPW 1.22 in English: their titles, briefings and victory conditions, which the
// executable has only in Japanese (ScenarioSelection), translated for the UI.
internal static class SingleplayerTexts
{
	// How the victory conditions start, as the briefing's view finds them (BriefingView).
	private const string ConditionsStart = "Victory Line : ";

	private static readonly Dictionary<
		int,
		(string Title, string Briefing, string Conditions)
	> English = new()
	{
		[1] = (
			"Grand Exercise off Hawaii",
			"A practice scenario. Target ships are floating south of Hawaii: attack them. Historically, after this the U.S. fleet did not return to its naval bases on the mainland, but anchored in Pearl Harbor, Hawaii.",
			"You win when all the target ships are sunk. You lose when the time runs out."
		),
		[2] = (
			"Pearl Harbor Attack",
			"Attack the U.S. Navy's ships that should be in Pearl Harbor, Hawaii. The Japan Navy has several fleet carriers. As in history, an attack by aircraft is best.",
			"Inflict heavy damage on the U.S. ships in Pearl Harbor. You lose when the time runs out."
		),
		[3] = (
			"Defense of Wake Island",
			"Having struck Pearl Harbor, the Japan Navy's carrier fleet heads for home. But a few of its carriers have been sent to take Wake Island. Command the island's air units and defend it. Its surrender is only a matter of time, but you should do the enemy what damage you can.",
			"You win by holding Wake Island's infantry base until the time is up, or by sinking an enemy carrier. You lose if the base or your air power is destroyed."
		),
		[4] = (
			"Battle of the Coral Sea",
			"The first carrier battle in naval history, fought only by the aircraft the carriers launched, without the fleets ever sighting each other. Historically the Japan Navy sank a U.S. carrier, but the battle halted its main objective, the invasion of Port Moresby.",
			"Sink one enemy carrier first. You lose when the time runs out."
		),
		[5] = (
			"Fateful Midway",
			"Aiming at a strategic surprise, the Japan Navy begins its invasion of Midway. But it finds U.S. carriers nearby, and the battle of the two navies' carrier forces begins. Historically it was a great U.S. victory, but in this scenario the Japan Navy will not make the same mistakes.",
			"Destroy all the enemy carriers. You lose when the time runs out."
		),
		[6] = (
			"Battle of the Santa Cruz Islands",
			"A battle in the struggle for Guadalcanal. The Japanese army and navy commit all their forces to retake the island, and the U.S. Navy concentrates its strength in these waters too. Historically the Japan Navy sank a U.S. carrier and a destroyer and lost no ships, but in this battle it lost most of its trained pilots.",
			"Destroy all the enemy carriers. You lose when the time runs out."
		),
		[7] = (
			"Naval South Pacific War",
			"After the carrier battle of the Santa Cruz Islands, both navies must rebuild their carrier fleets. But the fighting is not over. The U.S. Navy must hold Guadalcanal at all costs, and the Japan Navy attacks the island's airfield with all its strength, however many ships it sends to the bottom of Ironbottom Sound.",
			"The U.S. Navy wins by holding Guadalcanal's airfield until the time is up, or by destroying the Japan Navy."
		),
		[8] = (
			"Offensive on Truk",
			"The U.S. Navy's great offensive begins, and the Japan Navy meets it. Victory in this great battle means driving the Japan Navy out of the Pacific for good. At the start, some ships are under repair, and they stay anchored throughout. The Japan Navy must protect these anchored ships at Truk and Wake.",
			"You win if any anchored ships remain when the time is up. You lose if all of them are sunk."
		),
		[9] = (
			"Battle off the Marianas",
			"The Japan Navy's last great counterattack begins: the battle for Saipan in the Marianas. By now the Japan Navy has been all but driven out of the South Pacific, and if Saipan is lost, the Japanese homeland comes within reach of the enemy's bombers. The enemy fleet's position is known: straight to the right of the Japanese fleet on the map.",
			"You win by destroying all the enemy carriers. You lose when the time runs out or all your carriers are lost."
		),
		[10] = (
			"Interception over Japan",
			"The U.S. forces that landed on Saipan at once complete an airfield and base many long-range bombers there. This marks the final phase of the war that began at Pearl Harbor, for their targets are the cities of Japan, above all the capital, Tokyo.",
			"The Japan Navy loses if all its cities are lost, the U.S. Navy if all its air power is lost. A draw when the time runs out."
		),
		[100] = (
			"The Silent Fleet",
			"The Japanese army fights hard on Guadalcanal, but supplying the island is extremely difficult, above all because of the attacks of carrier aircraft. Submarines are called on to whittle down the enemy's strength. Lead the silent fleet and sink the enemy carrier Wasp.",
			"You lose if the Japanese bases on Guadalcanal or your submarine fleet are destroyed. You win by sinking the enemy carrier."
		),
		[101] = (
			"Decisive Fleet Battle (Japan Navy)",
			"A decisive battle of surface forces alone: only battleships, cruisers and destroyers take part, with the same number of each on both sides.",
			"You win by destroying all the enemy's battleships and cruisers."
		),
		[102] = (
			"Decisive Fleet Battle (U.S. Navy)",
			"A decisive battle of surface forces alone: only battleships, cruisers and destroyers take part, with the same number of each on both sides. This scenario is the one above it with the side you command changed.",
			"You win by destroying all the enemy's battleships and cruisers."
		),
		[103] = (
			"Beyond Time and Space! Deep Blue Air Group",
			"In the thick of the struggle for Guadalcanal, Commodore William Konpeki, aboard a U.S. cruiser, was killed when Japanese ships attacked, for want of ships. But he crossed space and time and came back to life before the Pacific War! Vowing that the tragedy of Pearl Harbor would not happen again, he secretly founded a society called Deep Blue, and with its help took command of Hawaii's air defense. His second morning of December 7 is about to dawn.",
			"You win by wiping out the enemy's air units. You lose if all the ships in Pearl Harbor are sunk."
		),
		[104] = (
			"Land on Guadalcanal!",
			"Ships of every size lie at anchor at Kwajalein, among them the destroyers and transports under your command, now making ready to sortie. The army's soldiers are already aboard the transports, and the sailors on the destroyers' decks hurry through the last preparations. Your objective is to land the soldiers on Guadalcanal and wipe out the enemy garrison.",
			"You win by destroying the enemy's ground forces on Guadalcanal within the time limit. Anything else is a defeat."
		),
		[105] = (
			"Decisive Carrier Battle (Japan Navy)",
			"A battle of carriers together with other ships. The forces are about equal.",
			"You win by destroying the enemy's carriers, and lose if yours are destroyed."
		),
		[106] = (
			"Decisive Carrier Battle (U.S. Navy)",
			"A battle of carriers together with other ships. The forces are about equal.",
			"You win by destroying the enemy's carriers, and lose if yours are destroyed."
		),
		[107] = (
			"Shoot Down Yamamoto!",
			"Your mission is to shoot down a single enemy plane. Launch carrier fighters from the carrier under your command and fly them straight north. They should reach the sky over a large island: just where the large plane carrying the Japan Navy's commander in chief, Admiral Yamamoto, will pass. Trust intelligence on this.",
			"You win by shooting down the large plane carrying Yamamoto."
		),
		[108] = (
			"Samurai of the Sky",
			"The war in the South Pacific is all but decided. Guadalcanal and its airfield are wholly in American hands, and the U.S. Navy is tremendously strong. Even so, the Rabaul air group has been ordered to stop the enemy's advance: a battle like holding back a tidal wave of an army with bare hands.",
			"Keep at least one Japanese base on the islands east of Rabaul's airfield until the time is up."
		),
		[109] = (
			"Special Orders! Deep Blue Air Group",
			"Commodore William Konpeki, reborn across space and time. Thanks to him, the tragedy of Pearl Harbor was kept to a minimum. But the Japan Navy is still a formidable enemy, launching skillful offensives everywhere. To put it on the defensive, the Deep Blue Air Group has been ordered to raid Yokosuka.",
			"Sink the carriers, battleships, cruisers and transports anchored at Yokosuka, and come back to around where you started. You lose when the time runs out."
		),
		[200] = (
			"Battles for Guadalcanal",
			"It is clear now: the enemy on Guadalcanal is no small reconnaissance in force. The U.S. Navy means to make the island a bridgehead and conquer the South Pacific. An enemy task force has been found too, and these waters are becoming the scene of the decisive battle between Japan and America. Reinforcements for your damaged forces have arrived: with them, deal the enemy a killing blow!",
			"You win by destroying all the enemy's facilities on Guadalcanal. You lose when the time runs out or if you lose a carrier."
		),
		[201] = (
			"The Carrier Hiryu Still Stands",
			"A single strike by enemy dive bombers has wrapped the carriers Akagi, Kaga and Soryu in flames! Taking Midway is out of the question now. There is only one objective: to avenge the three stricken carriers. A killing blow is needed from the aircraft of the carrier Hiryu, still unharmed!",
			"The Japan Navy wins by sinking a U.S. carrier. Anything else is a defeat."
		),
		[202] = (
			"Tokyo Bombing",
			"On Saipan and Tinian, the great silver wings of four-engined bombers stand in rows, so many that they show the awesome might of American industry. Command them to bomb and destroy Tokyo, the most important strategic target of this war.",
			"You lose if you cannot destroy Tokyo's cities within the time limit."
		),
		[203] = (
			"The Glorious Carrier Enterprise",
			"The aircraft carrier Enterprise, by all accounts the most distinguished ship of the Pacific War. Damaged again and again, she always returned to the line and sent many enemy ships to the bottom: a truly glorious carrier. Here she takes on the Japan Navy closing in on Midway.",
			"You win by destroying many enemy ships. You lose if the pillboxes on Midway are destroyed."
		),
		[204] = (
			"Absolute Air Supremacy! Deep Blue Air Group",
			"The U.S. Navy has landed on Guadalcanal. The island must be defended until the marines have built a base there for the next advance. But Japanese air raids have made its airfield useless, and our carriers have left these waters. And the enemy's air attacks will only grow fiercer. It was at such a time that Konpeki was summoned to headquarters at Nouméa.",
			"Hold the ground facilities on Guadalcanal until the time is up."
		),
		[205] = (
			"Great Rescue! Deep Blue Air Group",
			"The Japan Navy has begun a counteroffensive against Bougainville. Having advanced too fast, the island's airfield is in danger. The Deep Blue Air Group has been ordered to rescue its pilots and other personnel with strategic bombers. William Konpeki's battle begins.",
			"Land the strategic bombers from Nouméa at Bougainville's airfield, and bring them back to the airfield at Nouméa."
		),
		[206] = (
			"Sink the Yamato",
			"The fighting on Okinawa is coming to an end. But word has come that the giant battleship Yamato, the symbol of the Japan Navy, is sailing through the Bungo Channel toward Okinawa, probably to give supporting fire to the main island.",
			"You win by sinking the battleship Yamato. You lose when the time runs out."
		),
		[207] = (
			"Morning Fleet.",
			"The defense of Guadalcanal was in crisis. The Japan Navy was relentless and strong. But a plan to turn the tide was under way under Captain Sharan Tsunku: to put back into the line a fleet made of ships once struck from the active force. The Morning Fleet. sortied to destroy Rabaul.",
			"Destroy Rabaul's airfield, and come back to around where you started. You lose if you lose all your battleships and cruisers."
		),
		[208] = (
			"Fierce Fighting! Deep Blue Air Group",
			"The Cactus Air Force defending Guadalcanal has been wiped out! Konpeki could not shake off an ominous thought: the Japan Navy is far too strong! Could someone like himself be commanding it? But there was no time to think: orders came from headquarters at Nouméa. Deep Blue Air Group, hold Guadalcanal to the last!",
			"You win by defending Guadalcanal's infantry base until the time is up, or by destroying the enemy."
		),
		[209] = (
			"Invasion of Midway",
			"The Doolittle Raid laid bare how weak the homeland's defenses were. Feeling the need to remove that threat before fighting in the South Seas, the Japan Navy launches an operation to take complete control of the central Pacific. The target is Midway: occupy and hold the island and set up a defensive line, and the objective will be achieved.",
			"When the time is up, keep at least one base on Midway, and have destroyed the enemy's ground force bases."
		),
		[300] = (
			"Doolittle Bombing",
			"To raise the nation's morale as America entered the war, a fantastic plan was devised: to load a fleet carrier not with single-engine attack planes but with far larger bombers, and bomb Japan directly. On April 18, 1942, at 8:30 a.m., the bombers under Major Jimmy Doolittle took off from the carrier Hornet.",
			"Destroy two of Tokyo's cities within the time limit, and gather all your remaining bombers at the bottom left of the map."
		),
		[301] = (
			"Invasion of Port Moresby",
			"The Japanese army and navy have spread their conquests at an unstoppable pace: the Philippines, Singapore, Indonesia. Now the navy's leaders conceive a grand plan: to cut the lines between America and Australia. To that end, the invasion of Port Moresby, essential to cutting them, has been launched.",
			"Within the time limit, destroy two enemy carriers and all of Port Moresby's infantry base, pillboxes and fortress, but leave the city standing, and build at least one ground base next to it."
		),
		[302] = (
			"Target: Midway",
			"Major Doolittle's bombing of Tokyo meant nothing tactically, and for all the words \"Tokyo bombed\", its damage could be judged slight. But its effect on the navy's leaders, who felt they had let the bombing happen, was enormous, and it drove them to invade Midway.",
			"Destroy the ground facilities on Midway within the time limit, without losing a single carrier."
		),
		[303] = (
			"Mission Impossible! Deep Blue Air Group",
			"From the bridge of a destroyer, Konpeki saw a transport approaching, flashing a top-secret Morse lamp that only he and a few others knew. \"Good morning, Mr. Konpeki. Many of our bombers are being lost to the new interceptor Shinden, based at Kwajalein Atoll. Your mission this time is to steal that new interceptor and deliver it to Nouméa. Land the soldiers of the transports at Nouméa next to the enemy's airfield, and you should be able to destroy everything but the plane we want and take over the airfield. As always, this message will self-destruct...\"",
			""
		),
		[304] = (
			"Battle in Japanese Waters",
			"The U.S. Navy is closing in on Japanese waters, and the gap between the two sides' strength keeps widening. But if this battle is lost, Japan has nothing left. The absolute national defense zone must not be breached. The defense of Japan depends entirely on the battles to come.",
			"Hold your ground facilities until the time is up."
		),
		[305] = (
			"The Battle of the Battleship Yamato",
			"U.S. forces have landed on Okinawa in tremendous strength. The outcome of this war is already decided: there is no victory left for Japan, and any battle fought from now on will be in vain. Even so, on April 6, 1945, believing in Japan's victory and to save Okinawa, the Japan Navy's pride, the giant battleship Yamato, sortied.",
			"Annihilate the U.S. bases on Okinawa within the time limit."
		),
		[306] = (
			"The Silent Fleet Sorties Again",
			"Before the U.S. Navy's great counteroffensive, the Japan Navy's strength is slowly ebbing away. But Guadalcanal must be retaken. Transports from Truk will soon reach Kwajalein: they must be brought safely to Guadalcanal, and the enemy's attacks held off. The silent fleet has sortied once more.",
			"Within the time limit, annihilate the enemy on Guadalcanal, and build at least two Japanese ground facilities there."
		),
	};

	// The scenario, in the language chosen (Texts.Language); as the executable has it in Japanese, or where it has no
	// translation.
	private static ScenarioEntry Of(ScenarioEntry scenario)
	{
		if (
			Texts.Language == Language.Japanese
			|| !English.TryGetValue(scenario.Number, out var english)
		)
			return scenario;

		var briefing =
			english.Conditions.Length == 0
				? [english.Briefing]
				: new[] { english.Briefing, ConditionsStart + english.Conditions };
		return scenario with { Title = english.Title, Briefing = briefing };
	}

	// The sets of scenarios, in the language chosen.
	public static IReadOnlyList<ScenarioSet> Of(IReadOnlyList<ScenarioSet> sets)
	{
		return [.. sets.Select(set => set with { Scenarios = [.. set.Scenarios.Select(Of)] })];
	}
}
