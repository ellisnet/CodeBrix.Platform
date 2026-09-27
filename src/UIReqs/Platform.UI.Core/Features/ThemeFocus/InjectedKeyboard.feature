Feature: InjectedKeyboard
	A key an application injects with InputInjector.InjectKeyboardInput must take the path a key of
	the real keyboard takes: it reaches the focused element as its Preview and bubbling key events,
	types its character into a focused TextBox, moves the focus on Tab, presses the Button the
	keyboard is on, and invokes keyboard accelerators and access keys.

Scenario: Injected keys type their characters into the focused TextBox
	Given the application shows a TextBox named "entry" with:
		| Property        | Value |
		| Width           | 600   |
		| Height          | 70    |
		| FontSize        | 32    |
		| Foreground      | Blue  |
		| Background      | White |
		| BorderThickness | 0     |
	When "entry" is tapped
	And the text "Hello, World 42!" is typed with injected keys
	And the frame is captured
	Then the Text of "entry" is "Hello, World 42!"
	And the region of "entry" has ink

Scenario: Injected keys type what the panel's keyboard types
	Given the application shows a StackPanel named "column" 600 by 200
	And the layout "column" holds a TextBox named "typed" with:
		| Property | Value |
		| FontSize | 24    |
	And the layout "column" holds a TextBox named "injected" with:
		| Property | Value |
		| FontSize | 24    |
	When "typed" is tapped
	And the text "abc XYZ 789" is typed
	And "injected" is tapped
	And the text "abc XYZ 789" is typed with injected keys
	Then the Text of "typed" is "abc XYZ 789"
	And the Text of "injected" is "abc XYZ 789"

Scenario: Injected Unicode characters are typed as they are
	Given the application shows a TextBox named "entry" with:
		| Property | Value |
		| Width    | 600   |
		| Height   | 70    |
		| FontSize | 32    |
	When "entry" is tapped
	And the text "naïve café" is typed with injected Unicode characters
	Then the Text of "entry" is "naïve café"

Scenario: An injected key raises the key events of the focused element
	Given the application shows a TextBox named "entry" with:
		| Property | Value |
		| Width    | 600   |
		| Height   | 70    |
	And the key events of "entry" are recorded
	When "entry" is tapped
	And the key "A" is injected
	Then the PreviewKeyDown of "entry" was raised at least once
	And the KeyDown of "entry" was raised at least once
	And the PreviewKeyUp of "entry" was raised at least once
	And the KeyUp of "entry" was raised at least once
	And the Text of "entry" is "a"

Scenario: An injected Tab moves the keyboard focus to the next control
	Given the application shows a StackPanel named "row" 600 by 200
	And the layout "row" holds a Button named "first" with:
		| Property | Value |
		| Content  | First |
	And the layout "row" holds a Button named "second" with:
		| Property | Value  |
		| Content  | Second |
	And the keyboard focus is given to "first"
	When the key "Tab" is injected
	Then "second" has keyboard focus
	And "first" does not have keyboard focus

Scenario: An injected Enter presses the Button the keyboard is on
	Given the application shows a Button named "go" with:
		| Property | Value |
		| Width    | 300   |
		| Height   | 120   |
		| Content  | Go    |
	And the keyboard focus is given to "go"
	When the key "Enter" is injected
	Then the Click of "go" was raised once

Scenario: An injected Control chord invokes a keyboard accelerator and types nothing
	Given the application shows a StackPanel named "column" 600 by 240
	And the layout "column" holds a TextBox named "entry" with:
		| Property | Value |
		| FontSize | 24    |
	And the layout "column" holds a Button named "save" with:
		| Property | Value |
		| Content  | Save  |
	And "save" has the keyboard accelerator Control+"S"
	When "entry" is tapped
	And the key "S" is injected with the Control key held down
	Then the Invoked of "save" was raised at least once
	And the Click of "save" was raised once
	And the Text of "entry" is ""

Scenario: The same key injected without Control types instead of invoking the accelerator
	Given the application shows a StackPanel named "column" 600 by 240
	And the layout "column" holds a TextBox named "entry" with:
		| Property | Value |
		| FontSize | 24    |
	And the layout "column" holds a Button named "save" with:
		| Property | Value |
		| Content  | Save  |
	And "save" has the keyboard accelerator Control+"S"
	When "entry" is tapped
	And the key "S" is injected
	Then the Invoked of "save" was never raised
	And the Click of "save" was not raised
	And the Text of "entry" is "s"

Scenario: Injected Alt and an access key press the Button that has the access key
	Given the application shows a StackPanel named "row" 600 by 200
	And the layout "row" holds a Button named "first" with:
		| Property | Value |
		| Content  | First |
	And the layout "row" holds a Button named "go" with:
		| Property | Value |
		| Content  | Go    |
	And "go" has the access key "G"
	And the keyboard focus is given to "first"
	When the key "Menu" is injected
	And the key "G" is injected
	Then the Click of "go" was raised once
	And the Click of "first" was not raised

Scenario: A keyboard accelerator added to a loaded element is invoked by the panel's keyboard too
	Given the application shows a StackPanel named "column" 600 by 240
	And the layout "column" holds a Button named "first" with:
		| Property | Value |
		| Content  | First |
	And the layout "column" holds a Button named "save" with:
		| Property | Value |
		| Content  | Save  |
	And "save" has the keyboard accelerator Control+"S"
	And the keyboard focus is given to "first"
	When the key "S" is pressed with the Control key held down
	Then the Invoked of "save" was raised at least once
	And the Click of "save" was raised once
	And the Click of "first" was not raised
