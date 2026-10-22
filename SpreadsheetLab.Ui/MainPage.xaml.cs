using System.Text.Json;

namespace SpreadsheetLab.Ui;

public partial class MainPage : ContentPage
{
    private const double CellWidth = 125;
    private const double CellHeight = 42;
    private const double HeaderWidth = 48;
    private const double HeaderHeight = 36;

    private readonly Dictionary<string, string> cells = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Entry> cellEntries = new(StringComparer.OrdinalIgnoreCase);

    private int rowCount = 12;
    private int columnCount = 8;
    private string activeCell = "A1";

    public MainPage()
    {
        InitializeComponent();
        CreateGrid();
    }

    private void CreateGrid()
    {
        SpreadsheetGrid.Children.Clear();
        SpreadsheetGrid.RowDefinitions.Clear();
        SpreadsheetGrid.ColumnDefinitions.Clear();
        cellEntries.Clear();

        SpreadsheetGrid.RowDefinitions.Add(new RowDefinition(HeaderHeight));
        SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition(HeaderWidth));

        for (int column = 0; column < columnCount; column++)
        {
            SpreadsheetGrid.ColumnDefinitions.Add(new ColumnDefinition(CellWidth));
            AddHeader(GetColumnName(column), column + 1, 0);
        }

        for (int row = 1; row <= rowCount; row++)
        {
            SpreadsheetGrid.RowDefinitions.Add(new RowDefinition(CellHeight));
            AddHeader(row.ToString(), 0, row);

            for (int column = 0; column < columnCount; column++)
            {
                string address = $"{GetColumnName(column)}{row}";
                var entry = new Entry
                {
                    AutomationId = address,
                    Text = cells.GetValueOrDefault(address, string.Empty),
                    BackgroundColor = Colors.Transparent,
                    TextColor = Color.FromArgb("#172B4D"),
                    FontSize = 14,
                    Margin = 0,
                    HorizontalTextAlignment = TextAlignment.Start
                };

                var cellBorder = new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#D9E0EA"),
                    StrokeThickness = 1,
                    Padding = new Thickness(6, 0),
                    Content = entry
                };

                entry.Focused += CellEntry_Focused;
                entry.Unfocused += CellEntry_Unfocused;
                Grid.SetColumn(cellBorder, column + 1);
                Grid.SetRow(cellBorder, row);
                SpreadsheetGrid.Children.Add(cellBorder);
                cellEntries[address] = entry;
            }
        }

        SelectCell(activeCell);
    }

    private void AddHeader(string text, int column, int row)
    {
        var label = new Label
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#E9EEF5"),
            TextColor = Color.FromArgb("#344563"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontAttributes = FontAttributes.Bold,
            FontSize = 13
        };

        Grid.SetColumn(label, column);
        Grid.SetRow(label, row);
        SpreadsheetGrid.Children.Add(label);
    }

    private void CellEntry_Focused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.AutomationId))
        {
            SelectCell(entry.AutomationId);
        }
    }

    private void CellEntry_Unfocused(object? sender, FocusEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrWhiteSpace(entry.AutomationId))
        {
            StoreCell(entry.AutomationId, entry.Text);
            if (entry.AutomationId.Equals(activeCell, StringComparison.OrdinalIgnoreCase))
            {
                FormulaEntry.Text = entry.Text;
            }
        }
    }

    private void SelectCell(string address)
    {
        if (!cellEntries.ContainsKey(address))
        {
            address = "A1";
        }

        activeCell = address;
        ActiveCellLabel.Text = address;
        FormulaEntry.Text = cells.GetValueOrDefault(address, cellEntries[address].Text ?? string.Empty);
        StatusLabel.Text = $"Активна клітинка: {address}";
    }

    private void StoreCell(string address, string? value)
    {
        string text = value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            cells.Remove(address);
        }
        else
        {
            cells[address] = text;
        }
    }

    private void ApplyFormula()
    {
        string value = FormulaEntry.Text ?? string.Empty;
        StoreCell(activeCell, value);
        cellEntries[activeCell].Text = value;
        StatusLabel.Text = $"Дані введено в {activeCell}";
    }

    private void FormulaEntry_Completed(object? sender, EventArgs e)
    {
        ApplyFormula();
    }

    private void ApplyFormulaButton_Clicked(object? sender, EventArgs e)
    {
        ApplyFormula();
    }

    private async void NewButton_Clicked(object? sender, EventArgs e)
    {
        bool createNew = await DisplayAlert("Нова таблиця", "Очистити поточну таблицю?", "Так", "Ні");
        if (!createNew)
        {
            return;
        }

        cells.Clear();
        rowCount = 12;
        columnCount = 8;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = "Створено нову таблицю";
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        SaveVisibleEntries();
        var data = new SheetData(rowCount, columnCount, cells);
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(GetSavePath(), json);
        StatusLabel.Text = "Таблицю збережено";
        await DisplayAlert("Збереження", "Таблицю збережено у локальному сховищі програми.", "OK");
    }

    private async void OpenButton_Clicked(object? sender, EventArgs e)
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            await DisplayAlert("Відкриття", "Збереженої таблиці ще немає.", "OK");
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(path);
            SheetData? data = JsonSerializer.Deserialize<SheetData>(json);
            if (data is null)
            {
                throw new JsonException();
            }

            rowCount = Math.Max(1, data.Rows);
            columnCount = Math.Max(1, data.Columns);
            cells.Clear();
            foreach (var item in data.Cells)
            {
                cells[item.Key] = item.Value;
            }

            activeCell = "A1";
            CreateGrid();
            StatusLabel.Text = "Таблицю відкрито";
        }
        catch (Exception)
        {
            await DisplayAlert("Помилка", "Не вдалося прочитати збережену таблицю.", "OK");
        }
    }

    private async void CalculateButton_Clicked(object? sender, EventArgs e)
    {
        SaveVisibleEntries();
        await DisplayAlert(
            "Обчислення",
            "Інтерфейс готовий. На наступному етапі сюди підключається створений ANTLR-обчислювач.",
            "OK");
    }

    private async void HelpButton_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "Довідка",
            "Виберіть клітинку та введіть вираз у клітинку або у верхнє поле. " +
            "Для варіанта 80 передбачені mmax, mmin, =, <, >, not, and, or та eqv.",
            "OK");
    }

    private async void AboutButton_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "Про програму",
            "Електронні таблиці. Лабораторна робота №1, варіант 80.",
            "OK");
    }

    private async void ExitButton_Clicked(object? sender, EventArgs e)
    {
        bool exit = await DisplayAlert("Вихід", "Завершити роботу програми?", "Так", "Ні");
        if (exit)
        {
            Application.Current?.Quit();
        }
    }

    private void AddRowButton_Clicked(object? sender, EventArgs e)
    {
        SaveVisibleEntries();
        rowCount++;
        CreateGrid();
        StatusLabel.Text = $"Рядків: {rowCount}";
    }

    private void RemoveRowButton_Clicked(object? sender, EventArgs e)
    {
        if (rowCount == 1)
        {
            return;
        }

        SaveVisibleEntries();
        for (int column = 0; column < columnCount; column++)
        {
            cells.Remove($"{GetColumnName(column)}{rowCount}");
        }

        rowCount--;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = $"Рядків: {rowCount}";
    }

    private void AddColumnButton_Clicked(object? sender, EventArgs e)
    {
        SaveVisibleEntries();
        columnCount++;
        CreateGrid();
        StatusLabel.Text = $"Стовпчиків: {columnCount}";
    }

    private void RemoveColumnButton_Clicked(object? sender, EventArgs e)
    {
        if (columnCount == 1)
        {
            return;
        }

        SaveVisibleEntries();
        string removedColumn = GetColumnName(columnCount - 1);
        for (int row = 1; row <= rowCount; row++)
        {
            cells.Remove($"{removedColumn}{row}");
        }

        columnCount--;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = $"Стовпчиків: {columnCount}";
    }

    private void ModeSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        ModeLabel.Text = e.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗИ";
        StatusLabel.Text = e.Value ? "Увімкнено перегляд значень" : "Увімкнено перегляд виразів";
    }

    private void SaveVisibleEntries()
    {
        foreach (var item in cellEntries)
        {
            StoreCell(item.Key, item.Value.Text);
        }
    }

    private static string GetColumnName(int columnIndex)
    {
        string name = string.Empty;
        int number = columnIndex + 1;

        while (number > 0)
        {
            int remainder = (number - 1) % 26;
            name = (char)('A' + remainder) + name;
            number = (number - 1) / 26;
        }

        return name;
    }

    private static string GetSavePath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "spreadsheet.json");
    }

    private sealed record SheetData(int Rows, int Columns, Dictionary<string, string> Cells);
}
