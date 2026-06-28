using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Tests.App.Views //Was previously: Uno.UI.Tests.App.Views
{
	public class MyPoco
	{
		private double _bogosity;

		public double Bogosity
		{
			get => _bogosity;
			set => _bogosity = value;
		}
	}
}
