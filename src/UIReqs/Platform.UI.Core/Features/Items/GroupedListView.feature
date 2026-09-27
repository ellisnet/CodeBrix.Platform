Feature: Grouped ListView
	A ListView bound to a grouped collection view, with a GroupStyle, shows a header above the items of each
	group: a ListViewHeaderItem built from the GroupStyle's header template. The items are still counted across
	all the groups, so tapping, selecting and scrolling to an item work as they do in an ungrouped list. While the
	list scrolls through a group, that group's header stays at the top of the list. (A list bound to a collection
	view starts with the view's current item - the first - selected, as in WinUI, so tapping item 1 changes nothing.)

Scenario: A grouped ListView shows a header above the items of each group
	Given the application shows a grouped ListView named "list" 320 by 400 with the groups:
		| Group      | Items          |
		| Fruit      | Apple, Pear    |
		| Vegetables | Leek           |
		| Nuts       | Almond, Walnut |
	When the frame is captured
	Then the ListView "list" holds 5 items
	And the ListView "list" shows 3 group headers
	And group header 1 of the ListView "list" shows "Fruit"
	And group header 2 of the ListView "list" shows "Vegetables"
	And group header 3 of the ListView "list" shows "Nuts"
	And group header 1 of the ListView "list" is above item 1
	And item 2 of the ListView "list" is above group header 2
	And group header 2 of the ListView "list" is above item 3
	And item 3 of the ListView "list" is above group header 3
	And group header 3 of the ListView "list" is above item 4
	And group header 1 of the ListView "list" has ink
	And item 4 of the ListView "list" carries the text "Almond"
	And item 4 of the ListView "list" has ink

Scenario: An empty group of a grouped ListView still shows its header
	Given the application shows a grouped ListView named "list" 320 by 400 with the groups:
		| Group | Items  |
		| Fruit | Apple  |
		| Empty |        |
		| Nuts  | Almond |
	When the frame is captured
	Then the ListView "list" holds 2 items
	And the ListView "list" shows 3 group headers
	And group header 2 of the ListView "list" shows "Empty"
	And item 1 of the ListView "list" is above group header 2
	And group header 3 of the ListView "list" is above item 2

Scenario: A grouped ListView whose GroupStyle hides empty groups shows no header for an empty group
	Given the application shows a grouped ListView named "list" 320 by 400 that hides empty groups with the groups:
		| Group | Items  |
		| Fruit | Apple  |
		| Empty |        |
		| Nuts  | Almond |
	When the frame is captured
	Then the ListView "list" holds 2 items
	And the ListView "list" shows 2 group headers
	And group header 1 of the ListView "list" shows "Fruit"
	And group header 2 of the ListView "list" shows "Nuts"
	And group header 2 of the ListView "list" is above item 2

Scenario: A ListView bound to groups without a GroupStyle shows their items and no header
	Given the application shows a ListView named "list" 320 by 400 bound to the groups without a GroupStyle:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek        |
	When the frame is captured
	Then the ListView "list" holds 3 items
	And the ListView "list" shows 0 group headers
	And item 3 of the ListView "list" carries the text "Leek"
	And item 3 of the ListView "list" has ink

Scenario: Tapping items in two groups of a grouped ListView moves the selection across the groups
	Given the application shows a grouped ListView named "list" 320 by 400 with the groups:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek, Kale  |
	When item 1 of the ListView "list" is tapped
	And item 4 of the ListView "list" is tapped
	And the frame is captured
	Then the SelectedIndex of the ListView "list" is 3
	And item 4 of the ListView "list" is selected
	And item 1 of the ListView "list" is not selected
	And item 4 of the ListView "list" carries the text "Kale"
	And item 4 of the ListView "list" is filled with "Accent"
	And item 1 of the ListView "list" is not filled with "Accent"
	And the SelectionChanged of the ListView "list" was raised 1 times

Scenario: The Down key moves the selection from the last item of a group to the first item of the next
	Given the application shows a grouped ListView named "list" 320 by 400 with the groups:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek, Kale  |
	When item 2 of the ListView "list" is tapped
	And the key "Down" is pressed
	And the frame is captured
	Then the SelectedIndex of the ListView "list" is 2
	And item 3 of the ListView "list" is selected
	And item 3 of the ListView "list" carries the text "Leek"
	And item 2 of the ListView "list" is not selected

Scenario: Scrolling an item of a later group into view shows it below its group's header
	Given the application shows a grouped ListView named "list" 320 by 240 with the groups:
		| Group   | Items                  |
		| Group 1 | A1, A2, A3, A4, A5, A6 |
		| Group 2 | B1, B2, B3, B4, B5, B6 |
		| Group 3 | C1, C2, C3, C4, C5, C6 |
		| Group 4 | D1, D2, D3, D4, D5, D6 |
		| Group 5 | E1, E2, E3, E4, E5, E6 |
	When item 26 of the ListView "list" is scrolled into view
	And the frame is captured
	Then the ListView "list" is no longer scrolled to its top
	And item 26 of the ListView "list" carries the text "E2"
	And item 26 of the ListView "list" is inside its viewport
	And item 26 of the ListView "list" has ink
	And the group header "Group 5" of the ListView "list" is above item 25

Scenario: Scrolling the first item of a later group into view shows that group's header
	Given the application shows a grouped ListView named "list" 320 by 240 with the groups:
		| Group   | Items                  |
		| Group 1 | A1, A2, A3, A4, A5, A6 |
		| Group 2 | B1, B2, B3, B4, B5, B6 |
		| Group 3 | C1, C2, C3, C4, C5, C6 |
		| Group 4 | D1, D2, D3, D4, D5, D6 |
	When item 13 of the ListView "list" is scrolled into view
	And the frame is captured
	Then item 13 of the ListView "list" carries the text "C1"
	And item 13 of the ListView "list" is inside its viewport
	And the group header "Group 3" of the ListView "list" is above item 13

Scenario: The header of the group a ListView is scrolled into stays at the top of the list
	Given the application shows a grouped ListView named "list" 320 by 240 with the groups:
		| Group   | Items                  |
		| Group 1 | A1, A2, A3, A4, A5, A6 |
		| Group 2 | B1, B2, B3, B4, B5, B6 |
		| Group 3 | C1, C2, C3, C4, C5, C6 |
	When the ListView "list" is scrolled down by 120 pixels
	And the frame is captured
	Then the ListView "list" is no longer scrolled to its top
	And the group header "Group 1" of the ListView "list" sits at the top of its viewport

Scenario: A ListView scrolled far through its groups shows the header of the group it reached
	Given the application shows a grouped ListView named "list" 320 by 240 with the groups:
		| Group   | Items                  |
		| Group 1 | A1, A2, A3, A4, A5, A6 |
		| Group 2 | B1, B2, B3, B4, B5, B6 |
		| Group 3 | C1, C2, C3, C4, C5, C6 |
		| Group 4 | D1, D2, D3, D4, D5, D6 |
		| Group 5 | E1, E2, E3, E4, E5, E6 |
	When the ListView "list" is scrolled down by 700 pixels
	And the frame is captured
	Then the ListView "list" is no longer scrolled to its top
	And the group header "Group 3" of the ListView "list" sits at the top of its viewport
	And item 16 of the ListView "list" carries the text "C4"
	And item 16 of the ListView "list" has ink
