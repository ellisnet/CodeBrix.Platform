Feature: Grouped GridView
	A GridView bound to a grouped collection view, with a GroupStyle, shows each group as a header (a
	GridViewHeaderItem built from the GroupStyle's header template) with the group's tiles side by side under it;
	the next group starts on a line of its own. The tiles are still counted across all the groups, so tapping,
	selecting and scrolling to one work as they do in an ungrouped GridView. (A list bound to a collection view
	starts with the view's current item - the first - selected, as in WinUI.)

Scenario: A grouped GridView shows a header above the tiles of each group
	Given the application shows a grouped GridView named "tiles" 520 by 400 with the groups:
		| Group      | Items             |
		| Fruit      | Apple, Pear, Plum |
		| Vegetables | Leek, Kale        |
	When the frame is captured
	Then the GridView "tiles" holds 5 items
	And the GridView "tiles" shows 2 group headers
	And group header 1 of the GridView "tiles" shows "Fruit"
	And group header 2 of the GridView "tiles" shows "Vegetables"
	And group header 1 of the GridView "tiles" is above item 1
	And item 2 of the GridView "tiles" is to the right of item 1
	And item 1 of the GridView "tiles" is above group header 2
	And group header 2 of the GridView "tiles" is above item 4
	And item 5 of the GridView "tiles" is to the right of item 4
	And group header 1 of the GridView "tiles" has ink
	And item 4 of the GridView "tiles" carries the text "Leek"
	And item 4 of the GridView "tiles" has ink

Scenario: An empty group of a grouped GridView still shows its header
	Given the application shows a grouped GridView named "tiles" 520 by 400 with the groups:
		| Group | Items  |
		| Fruit | Apple  |
		| Empty |        |
		| Nuts  | Almond |
	When the frame is captured
	Then the GridView "tiles" holds 2 items
	And the GridView "tiles" shows 3 group headers
	And group header 2 of the GridView "tiles" shows "Empty"
	And item 1 of the GridView "tiles" is above group header 2
	And group header 3 of the GridView "tiles" is above item 2

Scenario: A grouped GridView whose GroupStyle hides empty groups shows no header for an empty group
	Given the application shows a grouped GridView named "tiles" 520 by 400 that hides empty groups with the groups:
		| Group | Items  |
		| Fruit | Apple  |
		| Empty |        |
		| Nuts  | Almond |
	When the frame is captured
	Then the GridView "tiles" holds 2 items
	And the GridView "tiles" shows 2 group headers
	And group header 2 of the GridView "tiles" shows "Nuts"
	And group header 2 of the GridView "tiles" is above item 2

Scenario: A GridView bound to groups without a GroupStyle shows their tiles and no header
	Given the application shows a GridView named "tiles" 520 by 400 bound to the groups without a GroupStyle:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek        |
	When the frame is captured
	Then the GridView "tiles" holds 3 items
	And the GridView "tiles" shows 0 group headers
	And item 3 of the GridView "tiles" is to the right of item 2

Scenario: Tapping tiles in two groups of a grouped GridView moves the selection across the groups
	Given the application shows a grouped GridView named "tiles" 520 by 400 with the groups:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek, Kale  |
	When the frame is captured as "unselected"
	And item 2 of the GridView "tiles" is tapped
	And item 4 of the GridView "tiles" is tapped
	And the frame is captured as "selected"
	Then the SelectedIndex of the GridView "tiles" is 3
	And item 4 of the GridView "tiles" is selected
	And item 2 of the GridView "tiles" is not selected
	And item 4 of the GridView "tiles" carries the text "Kale"
	And item 4 of the GridView "tiles" in frame "selected" differs from frame "unselected"
	And item 3 of the GridView "tiles" in frame "selected" is unchanged from frame "unselected"
	And the SelectionChanged of the GridView "tiles" was raised 2 times

Scenario: Scrolling a tile of a later group into view shows it below its group's header
	Given the application shows a grouped GridView named "tiles" 520 by 240 with the groups:
		| Group   | Items                  |
		| Group 1 | A1, A2, A3, A4, A5, A6 |
		| Group 2 | B1, B2, B3, B4, B5, B6 |
		| Group 3 | C1, C2, C3, C4, C5, C6 |
		| Group 4 | D1, D2, D3, D4, D5, D6 |
		| Group 5 | E1, E2, E3, E4, E5, E6 |
	When item 26 of the GridView "tiles" is scrolled into view
	And the frame is captured
	Then the GridView "tiles" is no longer scrolled to its top
	And item 26 of the GridView "tiles" carries the text "E2"
	And item 26 of the GridView "tiles" is inside its viewport
	And item 26 of the GridView "tiles" has ink
	And the group header "Group 5" of the GridView "tiles" is above item 25
