Feature: Grouped list layout options
	The ItemsStackPanel of a grouped ListView or GridView honours GroupHeaderPlacement and GroupPadding. With
	GroupHeaderPlacement Left each group's header sits beside the group, level with its first item, and the items
	take the breadth that remains; the next group starts below the longer of the two. GroupPadding insets every
	group: its top and bottom add space before and after each group, its left and right narrow the group. The
	default (Top, no padding) is the layout every grouped list had before.

Scenario: A grouped ListView with GroupHeaderPlacement Left shows each header beside its group
	Given the application shows a grouped ListView named "list" 320 by 400 with GroupHeaderPlacement Left and GroupPadding "0" with the groups:
		| Group      | Items          |
		| Fruit      | Apple, Pear    |
		| Vegetables | Leek           |
		| Nuts       | Almond, Walnut |
	When the frame is captured
	Then the ListView "list" holds 5 items
	And the ListView "list" shows 3 group headers
	And group header 1 of the ListView "list" shows "Fruit"
	And group header 1 of the ListView "list" is to the left of item 1
	And group header 1 of the ListView "list" is level with item 1
	And group header 2 of the ListView "list" is level with item 3
	And group header 3 of the ListView "list" is level with item 4
	And item 2 of the ListView "list" is above group header 2
	And group header 1 of the ListView "list" is above item 3
	And group header 2 of the ListView "list" has ink
	And item 1 of the ListView "list" carries the text "Apple"
	And item 1 of the ListView "list" has ink

Scenario: A grouped ListView with a GroupPadding insets every group
	Given the application shows a grouped ListView named "list" 320 by 400 with GroupHeaderPlacement Top and GroupPadding "24,16,24,16" with the groups:
		| Group      | Items       |
		| Fruit      | Apple, Pear |
		| Vegetables | Leek        |
	When the frame is captured
	Then the ListView "list" shows 2 group headers
	And group header 1 of the ListView "list" starts 16 pixels from the top of the list
	And group header 1 of the ListView "list" is above item 1
	And item 1 of the ListView "list" is inset 24 pixels on the left and 24 pixels on the right
	And item 3 of the ListView "list" is inset 24 pixels on the left and 24 pixels on the right
	And item 2 and group header 2 of the ListView "list" are 32 pixels apart
	And group header 2 of the ListView "list" is above item 3
	And item 3 of the ListView "list" has ink

Scenario: A grouped ListView with headers on the Left and a GroupPadding insets each group and its header
	Given the application shows a grouped ListView named "list" 320 by 400 with GroupHeaderPlacement Left and GroupPadding "8,12,8,12" with the groups:
		| Group | Items              |
		| Fruit | Apple, Pear, Plum  |
		| Nuts  | Almond             |
	When the frame is captured
	Then group header 1 of the ListView "list" starts 12 pixels from the top of the list
	And group header 1 of the ListView "list" is level with item 1
	And group header 1 of the ListView "list" is to the left of item 1
	And item 3 and group header 2 of the ListView "list" are 24 pixels apart
	And group header 2 of the ListView "list" is level with item 4
	And group header 2 of the ListView "list" is to the left of item 4

Scenario: ScrollIntoView reaches an item of a later group of a grouped ListView with headers on the Left
	Given the application shows a grouped ListView named "list" 320 by 240 with GroupHeaderPlacement Left and GroupPadding "0,8,0,8" with the groups:
		| Group | Items                                   |
		| A     | A1, A2, A3, A4, A5, A6                  |
		| B     | B1, B2, B3, B4, B5, B6                  |
		| C     | C1, C2, C3, C4, C5, C6                  |
		| D     | D1, D2, D3, D4, D5, D6                  |
	When item 20 of the ListView "list" is scrolled into view
	Then item 20 of the ListView "list" is inside its viewport
	And item 20 of the ListView "list" carries the text "D2"
	And the ListView "list" is no longer scrolled to its top

Scenario: A grouped GridView whose ItemsStackPanel puts headers on the Left shows each header beside its group
	Given the application shows a grouped GridView named "grid" 360 by 400 with GroupHeaderPlacement Left and GroupPadding "0,0,0,12" with the groups:
		| Group | Items      |
		| Red   | R1, R2     |
		| Blue  | B1         |
	When the frame is captured
	Then the GridView "grid" shows 2 group headers
	And group header 1 of the GridView "grid" is to the left of item 1
	And group header 1 of the GridView "grid" is level with item 1
	And group header 2 of the GridView "grid" is level with item 3
	And group header 2 of the GridView "grid" is to the left of item 3
	And group header 2 of the GridView "grid" has ink
