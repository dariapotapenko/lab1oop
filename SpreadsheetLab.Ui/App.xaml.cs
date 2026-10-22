namespace SpreadsheetLab.Ui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell())
		{
			Title = "Електронні таблиці",
			Width = 1180,
			Height = 760,
			MinimumWidth = 820,
			MinimumHeight = 600
		};
	}
}
