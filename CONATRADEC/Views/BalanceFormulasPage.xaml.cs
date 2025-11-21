using System;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
	public partial class BalanceFormulasPage : ContentPage
	{
		public BalanceFormulasPage()
		{
			// Load XAML explicitly to avoid ambiguity with generated InitializeComponent overloads
			Microsoft.Maui.Controls.Xaml.Extensions.LoadFromXaml(this, typeof(BalanceFormulasPage));
		}
	}
}
