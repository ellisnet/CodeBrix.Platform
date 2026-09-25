#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Composition;
using Windows.UI;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private Dictionary<CompositionAnimation, ICompositionTarget> _runningAnimations = new();
	private Dictionary<ICompositionTarget, int> _runningTargets = new();
	private LinkedList<ColorBrushTransitionState> _backgroundTransitions = new();

	internal bool? IsSoftwareRenderer { get; set; }

	internal bool IsAnimating => _runningAnimations.Count > 0;

	internal void RegisterAnimation(CompositionAnimation animation, CompositionObject visual)
	{
		if (animation.IsTrackedByCompositor)
		{
			if (visual is Visual { CompositionTarget: { } target })
			{
				_runningAnimations.Add(animation, target);

				if (_runningTargets.TryGetValue(target, out int count))
				{
					_runningTargets[target] = count + 1;
				}
				else
				{
					_runningTargets[target] = 1;
					target.RequestNewFrame();
				}

				if (this.Log().IsTraceEnabled())
				{
					this.Log().Trace($"Register running targets {target.GetHashCode():X8}={count} Animations={_runningAnimations.Count}");
				}
			}
		}
	}

	internal void UnregisterAnimation(CompositionAnimation animation, CompositionObject visual)
	{
		if (animation.IsTrackedByCompositor)
		{
			if (_runningAnimations.TryGetValue(animation, out var target))
			{
				_runningAnimations.Remove(animation);

				if (_runningTargets.TryGetValue(target, out int count))
				{
					if (this.Log().IsTraceEnabled())
					{
						this.Log().Trace($"Unregister running targets {target.GetHashCode():X8}={count - 1} Animations={_runningAnimations.Count}");
					}

					if (count == 1)
					{
						_runningTargets.Remove(target);
					}
					else
					{
						_runningTargets[target] = count - 1;
					}
				}
			}
			else
			{
				if (this.Log().IsDebugEnabled())
				{
					this.Log().Debug($"Cannot unregister unknown animation");
				}
			}
		}
	}

	internal void DeactivateBackgroundTransition(BorderVisual visual)
	{
		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			if (transitionVisual == visual)
			{
				current.Value = transition with { IsActive = false };
				break;
			}
		}
	}

	internal void RegisterBackgroundTransition(BorderVisual visual, Color fromColor, Color toColor, TimeSpan duration)
	{
		var start = TimestampInTicks;
		var end = start + duration.Ticks;

		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			if (transition.Visual == visual)
			{
				// when the background changes when already in a transition, the new transition
				// picks up from where the preexisting transition stopped UNLESS the preexisting
				// transition was inactive (i.e. an animation started during the transition.
				// In that case, just reactivate the preexisting transition.

				if (!transition.IsActive)
				{
					current.Value = transition with { IsActive = true };
					return;
				}

				fromColor = transition.CurrentColor;
				_backgroundTransitions.Remove(current);
				break;
			}
		}

		_backgroundTransitions.AddLast(new ColorBrushTransitionState(visual, fromColor, toColor, start, end, true));
	}

	internal bool TryGetEffectiveBackgroundColor(CompositionSpriteShape shape, out Color color)
	{
		foreach (var transition in _backgroundTransitions)
		{
			if (transition.Visual.IsMyBackgroundShape(shape))
			{
				if (transition.IsActive)
				{
					color = transition.CurrentColor;
					return true;
				}
				else
				{
					break;
				}
			}
		}

		color = default;
		return false;
	}

	/// <summary>
	/// Starts a frame: raises the animation frame of every running animation. The platform's renderer calls this
	/// right before it renders the tree of a root visual.
	/// </summary>
	internal void BeginFrame()
	{
		foreach (var animation in _runningAnimations.Keys.ToArray())
		{
			animation.RaiseAnimationFrame();
		}
	}

	/// <summary>
	/// Ends a frame: advances the background color transitions (invalidating the visuals they paint) and asks for
	/// another frame while animations or transitions are running. The platform's renderer calls this right after it
	/// rendered the tree of <paramref name="rootVisual"/>.
	/// </summary>
	/// <param name="rootVisual">The root visual that was rendered.</param>
	internal void EndFrame(ContainerVisual rootVisual)
	{
		var transitionsCount = _backgroundTransitions.Count;
		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			transitionVisual.InvalidatePaint();

			if (TimestampInTicks >= transition.EndTimestamp)
			{
				_backgroundTransitions.Remove(current);
			}
		}

		if (_runningAnimations.Count > 0 || transitionsCount > 0)
		{
			rootVisual.CompositionTarget?.RequestNewFrame();
		}
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
		visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
		visual.CompositionTarget?.RequestNewFrame();
	}
}
#endif
