Feature: TitleBar
	A TitleBar lays out, left to right, a back button, a pane toggle button, a left header, an icon, the title,
	the subtitle, the content and a right header, each shown only when the application asks for it. It is 32
	pixels tall, or 48 when it has a header or content. Its back button and pane toggle button raise BackRequested
	and PaneToggleRequested. It is an ordinary element: it draws where the application puts it.

Scenario: A TitleBar shows its title and its subtitle
	Given the application shows a TitleBar named "bar" with:
		| Property | Value |
		| Width    | 400   |
		| Title    | Mail  |
		| Subtitle | Inbox |
	When the frame is captured
	Then the "title" of the TitleBar "bar" shows "Mail"
	And the "subtitle" of the TitleBar "bar" shows "Inbox"
	And the "title" of the TitleBar "bar" has ink
	And the "subtitle" of the TitleBar "bar" has ink
	And the "title" of the TitleBar "bar" is to the left of its "subtitle"
	And the "back button" of the TitleBar "bar" is not shown
	And the "pane toggle button" of the TitleBar "bar" is not shown
	And the TitleBar "bar" is 32 pixels tall

Scenario: The back button of a TitleBar raises BackRequested
	Given the application shows a TitleBar named "bar" with:
		| Property            | Value |
		| Width               | 400   |
		| Title               | Mail  |
		| IsBackButtonVisible | True  |
	When the frame is captured
	And the "back button" of the TitleBar "bar" is tapped
	Then the "back button" of the TitleBar "bar" is shown
	And the "back button" of the TitleBar "bar" has ink
	And the "back button" of the TitleBar "bar" is to the left of its "title"
	And the BackRequested of the TitleBar "bar" was raised 1 times
	And the PaneToggleRequested of the TitleBar "bar" was raised 0 times

Scenario: A disabled back button of a TitleBar raises nothing
	Given the application shows a TitleBar named "bar" with:
		| Property            | Value |
		| Width               | 400   |
		| Title               | Mail  |
		| IsBackButtonVisible | True  |
		| IsBackButtonEnabled | False |
	When the "back button" of the TitleBar "bar" is tapped
	Then the "back button" of the TitleBar "bar" is shown
	And the back button of the TitleBar "bar" is disabled
	And the BackRequested of the TitleBar "bar" was raised 0 times

Scenario: The pane toggle button of a TitleBar raises PaneToggleRequested
	Given the application shows a TitleBar named "bar" with:
		| Property                  | Value |
		| Width                     | 400   |
		| Title                     | Mail  |
		| IsBackButtonVisible       | True  |
		| IsPaneToggleButtonVisible | True  |
	When the frame is captured
	And the "pane toggle button" of the TitleBar "bar" is tapped
	Then the "pane toggle button" of the TitleBar "bar" is shown
	And the "back button" of the TitleBar "bar" is to the left of its "pane toggle button"
	And the "pane toggle button" of the TitleBar "bar" is to the left of its "title"
	And the PaneToggleRequested of the TitleBar "bar" was raised 1 times
	And the BackRequested of the TitleBar "bar" was raised 0 times

Scenario: A TitleBar with headers and content is 48 pixels tall and lays them out left to right
	Given the application shows a TitleBar named "bar" with:
		| Property    | Value  |
		| Width       | 460    |
		| Title       | Mail   |
		| LeftHeader  | L      |
		| Content     | Search |
		| RightHeader | R      |
	When the frame is captured
	Then the TitleBar "bar" is 48 pixels tall
	And the "left header" of the TitleBar "bar" is shown
	And the "content" of the TitleBar "bar" is shown
	And the "right header" of the TitleBar "bar" is shown
	And the "left header" of the TitleBar "bar" is to the left of its "title"
	And the "title" of the TitleBar "bar" is to the left of its "content"
	And the "content" of the TitleBar "bar" is to the left of its "right header"
	And the "content" of the TitleBar "bar" has ink
	And the "right header" of the TitleBar "bar" has ink
