Feature: MenuFlyout
	A MenuFlyout offers a list of commands over the application. Tapping one runs that command
	and puts the menu away; tapping anywhere else puts the menu away and runs nothing.

Scenario: Showing a MenuFlyout puts its items over the application
	Given the application shows a Border named "anchor" 300 by 200 with Background "Silver"
	And a MenuFlyout named "menu" is attached to "anchor" with the items:
		| Item   |
		| Open   |
		| Save   |
		| Delete |
	When the flyout "menu" is shown at "anchor"
	And the frame is captured
	Then the flyout "menu" is open
	And a popup is open
	And the region of "Save" has ink

Scenario: Tapping an item of a MenuFlyout runs it and puts the menu away
	Given the application shows a Border named "anchor" 300 by 200 with Background "Silver"
	And a MenuFlyout named "menu" is attached to "anchor" with the items:
		| Item   |
		| Open   |
		| Save   |
		| Delete |
	When the flyout "menu" is shown at "anchor"
	And "Save" is tapped
	Then the Click of "Save" was raised once
	And the Click of "Open" was not raised
	And the flyout "menu" is closed
	And no popup is open

Scenario: Tapping away from a MenuFlyout runs nothing
	Given the application shows a Border named "anchor" 300 by 200 with Background "Silver"
	And a MenuFlyout named "menu" is attached to "anchor" with the items:
		| Item   |
		| Open   |
		| Save   |
		| Delete |
	When the frame is captured as "before"
	And the flyout "menu" is shown at "anchor"
	And a point far from "anchor" is tapped
	And the frame is captured as "dismissed"
	Then the flyout "menu" is closed
	And the Click of "Save" was not raised
	And nothing outside "anchor" changed between frames "dismissed" and "before"
