Feature: ListBox
	A ListBox shows a list of items the user can pick from, one ListBoxItem per item. In Single selection mode a
	tap or the arrow keys pick one item; in Multiple mode each tap (or Space) toggles an item; in Extended mode a
	tap picks one item, Shift and an arrow key extend the selection from the anchor, Control and an arrow key only
	move the focus, and Control+A selects everything. A selected item is painted with the selection colour.

Scenario: A ListBox draws an item for every item it holds
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When the frame is captured
	Then the ListBox "box" holds 4 items
	And item 1 of the ListBox "box" carries the text "Alpha"
	And item 4 of the ListBox "box" carries the text "Delta"
	And item 1 of the ListBox "box" has ink
	And item 4 of the ListBox "box" has ink
	And the SelectedIndex of the ListBox "box" is -1

Scenario: Tapping an item of a ListBox selects it and paints it
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When the frame is captured as "unselected"
	And item 2 of the ListBox "box" is tapped
	And the frame is captured as "selected"
	Then the SelectedIndex of the ListBox "box" is 1
	And the ListBox "box" has selected "Beta"
	And item 2 of the ListBox "box" is selected
	And the SelectionChanged of the ListBox "box" was raised 1 times
	And item 2 of the ListBox "box" in frame "selected" differs from frame "unselected"
	And item 3 of the ListBox "box" in frame "selected" is unchanged from frame "unselected"

Scenario: Tapping another item of a single-selection ListBox moves the selection
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When item 2 of the ListBox "box" is tapped
	And item 4 of the ListBox "box" is tapped
	Then the SelectedIndex of the ListBox "box" is 3
	And the ListBox "box" has selected "Delta"
	And item 4 of the ListBox "box" is selected
	And item 2 of the ListBox "box" is not selected
	And the SelectionChanged of the ListBox "box" was raised 2 times

Scenario: A ListBox in Multiple selection mode toggles every item a finger taps
	Given the application shows a ListBox named "box" with:
		| Property      | Value                     |
		| Width         | 320                       |
		| Height        | 300                       |
		| ItemLabels    | Alpha, Beta, Gamma, Delta |
		| SelectionMode | Multiple                  |
	When item 1 of the ListBox "box" is tapped
	And item 3 of the ListBox "box" is tapped
	And the frame is captured as "two"
	Then the ListBox "box" has selected "Alpha, Gamma"
	And item 1 of the ListBox "box" is selected
	And item 3 of the ListBox "box" is selected
	And item 2 of the ListBox "box" is not selected
	When item 1 of the ListBox "box" is tapped
	Then the ListBox "box" has selected "Gamma"
	And item 1 of the ListBox "box" is not selected
	And the SelectionChanged of the ListBox "box" was raised 3 times

Scenario: A tap in an Extended selection ListBox selects that item alone
	Given the application shows a ListBox named "box" with:
		| Property      | Value                     |
		| Width         | 320                       |
		| Height        | 300                       |
		| ItemLabels    | Alpha, Beta, Gamma, Delta |
		| SelectionMode | Extended                  |
	When item 2 of the ListBox "box" is tapped
	And item 4 of the ListBox "box" is tapped
	Then the ListBox "box" has selected "Delta"
	And item 4 of the ListBox "box" is selected
	And item 2 of the ListBox "box" is not selected

Scenario: The arrow keys move the focus and the selection of a single-selection ListBox
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When item 1 of the ListBox "box" is tapped
	And the key "Down" is injected
	And the key "Down" is injected
	Then the SelectedIndex of the ListBox "box" is 2
	And item 3 of the ListBox "box" has the focus
	And item 3 of the ListBox "box" is selected
	When the key "Up" is injected
	Then the SelectedIndex of the ListBox "box" is 1
	And item 2 of the ListBox "box" has the focus

Scenario: Home and End move to the first and the last item of a ListBox
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When item 2 of the ListBox "box" is tapped
	And the key "End" is injected
	Then the SelectedIndex of the ListBox "box" is 3
	And item 4 of the ListBox "box" has the focus
	When the key "Home" is injected
	Then the SelectedIndex of the ListBox "box" is 0
	And item 1 of the ListBox "box" has the focus

Scenario: Control and an arrow key move only the focus of a single-selection ListBox
	Given the application shows a ListBox named "box" with:
		| Property   | Value                     |
		| Width      | 320                       |
		| Height     | 300                       |
		| ItemLabels | Alpha, Beta, Gamma, Delta |
	When item 1 of the ListBox "box" is tapped
	And the key "Down" is injected with the Control key held down
	Then the SelectedIndex of the ListBox "box" is 0
	And item 2 of the ListBox "box" has the focus
	And item 1 of the ListBox "box" is selected

Scenario: Shift and an arrow key extend the selection of an Extended selection ListBox
	Given the application shows a ListBox named "box" with:
		| Property      | Value                     |
		| Width         | 320                       |
		| Height        | 300                       |
		| ItemLabels    | Alpha, Beta, Gamma, Delta |
		| SelectionMode | Extended                  |
	When item 2 of the ListBox "box" is tapped
	And the key "Down" is injected with the Shift key held down
	And the key "Down" is injected with the Shift key held down
	And the frame is captured
	Then the ListBox "box" has selected "Beta, Gamma, Delta"
	And item 4 of the ListBox "box" has the focus
	And item 1 of the ListBox "box" is not selected
	And item 3 of the ListBox "box" is selected
	When the key "Up" is injected with the Shift key held down
	Then the ListBox "box" has selected "Beta, Gamma"

Scenario: Space toggles the focused item of a Multiple selection ListBox
	Given the application shows a ListBox named "box" with:
		| Property      | Value                     |
		| Width         | 320                       |
		| Height        | 300                       |
		| ItemLabels    | Alpha, Beta, Gamma, Delta |
		| SelectionMode | Multiple                  |
	When item 1 of the ListBox "box" is tapped
	And the key "Down" is injected
	Then the ListBox "box" has selected "Alpha"
	And item 2 of the ListBox "box" has the focus
	When the key "Space" is injected
	Then the ListBox "box" has selected "Alpha, Beta"
	When the key "Space" is injected
	Then the ListBox "box" has selected "Alpha"

Scenario: Control+A selects every item of an Extended selection ListBox
	Given the application shows a ListBox named "box" with:
		| Property      | Value                     |
		| Width         | 320                       |
		| Height        | 300                       |
		| ItemLabels    | Alpha, Beta, Gamma, Delta |
		| SelectionMode | Extended                  |
	When item 3 of the ListBox "box" is tapped
	And the key "A" is injected with the Control key held down
	Then the ListBox "box" has selected "Alpha, Beta, Gamma, Delta"
	And item 1 of the ListBox "box" is selected

Scenario: ScrollIntoView brings a later item of a ListBox into its viewport
	Given the application shows a ListBox named "box" with:
		| Property   | Value                                                                           |
		| Width      | 320                                                                             |
		| Height     | 160                                                                             |
		| ItemLabels | One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Eleven, Twelve        |
	When the ListBox "box" scrolls item 11 into view
	Then item 11 of the ListBox "box" lies inside its viewport
