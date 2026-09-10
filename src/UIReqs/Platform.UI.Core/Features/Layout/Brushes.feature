Feature: Brushes
	A SolidColorBrush must paint an element one flat colour; a LinearGradientBrush must run
	from its first stop to its last along the axis it was given, with every colour in between
	lying between the two.

Scenario: A SolidColorBrush paints an element one flat colour
	Given the application shows a Border named "swatch" with:
		| Property   | Value     |
		| Width      | 400       |
		| Height     | 300       |
		| Background | #FF3366CC |
	When the frame is captured
	Then the region of "swatch" is uniformly "#FF3366CC"
	And the corner pixels of "swatch" are "#FF3366CC"

Scenario: A horizontal LinearGradientBrush runs from left to right
	Given the application shows a Border named "band" with:
		| Property           | Value    |
		| Width              | 400      |
		| Height             | 120      |
		| HorizontalGradient | Red,Blue |
	When the frame is captured
	Then the region of "band" runs as a horizontal gradient from "Red" to "Blue"

Scenario: A vertical LinearGradientBrush runs from top to bottom
	Given the application shows a Border named "band" with:
		| Property         | Value    |
		| Width            | 120      |
		| Height           | 400      |
		| VerticalGradient | Red,Blue |
	When the frame is captured
	Then the region of "band" runs as a vertical gradient from "Red" to "Blue"

Scenario: A shape can be filled with a gradient too
	Given the application shows a Rectangle named "band" with:
		| Property           | Value     |
		| Width              | 400       |
		| Height             | 120       |
		| HorizontalGradient | Lime,Navy |
	When the frame is captured
	Then the region of "band" runs as a horizontal gradient from "Lime" to "Navy"

Scenario: Replacing a gradient with a solid colour flattens the element
	Given the application shows a Border named "band" with:
		| Property           | Value    |
		| Width              | 400      |
		| Height             | 120      |
		| HorizontalGradient | Red,Blue |
	When the frame is captured as "gradient"
	Then the region of "band" runs as a horizontal gradient from "Red" to "Blue"
	When the Background of "band" is set to "Red"
	And the frame is captured as "solid"
	Then the region of "band" is uniformly "Red"
	And the region of "band" in frame "solid" differs from frame "gradient"
