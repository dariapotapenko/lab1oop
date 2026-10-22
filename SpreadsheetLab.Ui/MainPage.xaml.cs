using Antlr4.Runtime;
using SpreadsheetLab.Ui.Parsing;
using System.Numerics;
using System.Text.Json;

namespace SpreadsheetLab.Ui;

public partial class MainPage : ContentPage
{
    private const double CellWidth = 100;
    private const double CellHeight = 40;
    private const double HeaderWidth = 40;
    private const double HeaderHeight = 35;

    private readonly Dictionary<string, string> cells = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Entry> cellEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> calculatedValues = new(StringComparer.OrdinalIgnoreCase);

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
                    Text = GetDisplayedText(address),
                    IsReadOnly = ModeSwitch.IsToggled,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.Black,
                    FontSize = 14,
                    Margin = 0,
                    HorizontalTextAlignment = TextAlignment.Start
                };

                var cellBorder = new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Colors.LightGray,
                    StrokeThickness = 1,
                    Padding = new Thickness(5, 0),
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
            BackgroundColor = Colors.Gainsboro,
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
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
        if (ModeSwitch.IsToggled)
        {
            return;
        }

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
        FormulaEntry.Text = cells.GetValueOrDefault(address, string.Empty);
        StatusLabel.Text = $"Активна клітинка: {address}";
    }

    private void StoreCell(string address, string? value)
    {
        string text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            cells.Remove(address);
        }
        else
        {
            cells[address] = text;
        }

        calculatedValues.Clear();
    }

    private void ApplyFormula()
    {
        if (ModeSwitch.IsToggled)
        {
            ModeSwitch.IsToggled = false;
        }

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

        ModeSwitch.IsToggled = false;
        cells.Clear();
        calculatedValues.Clear();
        rowCount = 12;
        columnCount = 8;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = "Створено нову таблицю";
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        try
        {
            SaveVisibleEntries();
            var data = new SheetData(rowCount, columnCount, cells);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(GetSavePath(), json);
            StatusLabel.Text = "Таблицю збережено";
            await DisplayAlert("Збереження", "Таблицю збережено у локальному сховищі програми.", "OK");
        }
        catch (Exception exception)
        {
            await DisplayAlert("Помилка збереження", "Не вдалося зберегти таблицю: " + exception.Message, "OK");
        }
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

            ModeSwitch.IsToggled = false;
            rowCount = Math.Max(1, data.Rows);
            columnCount = Math.Max(1, data.Columns);
            cells.Clear();
            calculatedValues.Clear();
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

        try
        {
            CalculateAllCells();
            ModeSwitch.IsToggled = true;
            RefreshCellTexts();
            StatusLabel.Text = $"Обчислено клітинок: {calculatedValues.Count}";
        }
        catch (Exception exception)
        {
            await DisplayAlert("Помилка обчислення", exception.Message, "OK");
        }
    }

    private async void HelpButton_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "Довідка",
            "Підтримувані функції та оператори: mmax, mmin, =, <, >, not, and, or, eqv. " +
            "Вираз може містити цілі числа, круглі дужки та посилання на клітинки.",
            "OK");
    }

    private async void AboutButton_Clicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Про програму", "Програма для роботи з електронними таблицями.", "OK");
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
        ModeSwitch.IsToggled = false;
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
        ModeSwitch.IsToggled = false;
        for (int column = 0; column < columnCount; column++)
        {
            cells.Remove($"{GetColumnName(column)}{rowCount}");
        }

        calculatedValues.Clear();
        rowCount--;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = $"Рядків: {rowCount}";
    }

    private void AddColumnButton_Clicked(object? sender, EventArgs e)
    {
        SaveVisibleEntries();
        ModeSwitch.IsToggled = false;
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
        ModeSwitch.IsToggled = false;
        string removedColumn = GetColumnName(columnCount - 1);
        for (int row = 1; row <= rowCount; row++)
        {
            cells.Remove($"{removedColumn}{row}");
        }

        calculatedValues.Clear();
        columnCount--;
        activeCell = "A1";
        CreateGrid();
        StatusLabel.Text = $"Стовпчиків: {columnCount}";
    }

    private async void ModeSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            StoreAllEntries();
        }

        if (e.Value && calculatedValues.Count == 0 && cells.Count > 0)
        {
            try
            {
                CalculateAllCells();
            }
            catch (Exception exception)
            {
                ModeSwitch.IsToggled = false;
                await DisplayAlert("Помилка обчислення", exception.Message, "OK");
                return;
            }
        }

        ModeLabel.Text = e.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗИ";
        FormulaEntry.IsReadOnly = e.Value;
        RefreshCellTexts();
        StatusLabel.Text = e.Value ? "Показано значення" : "Показано вирази";
    }

    private void CalculateAllCells()
    {
        calculatedValues.Clear();
        var calculationPath = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string address in cells.Keys.ToArray())
        {
            EvaluateCell(address, calculationPath);
        }
    }

    private object EvaluateCell(string address, HashSet<string> calculationPath)
    {
        if (calculatedValues.TryGetValue(address, out object? value))
        {
            return value;
        }

        if (!cells.TryGetValue(address, out string? expression) || string.IsNullOrWhiteSpace(expression))
        {
            throw new Exception($"Клітинка {address} порожня");
        }

        if (!calculationPath.Add(address))
        {
            throw new Exception($"Циклічне посилання на клітинку {address}");
        }

        try
        {
            var input = new AntlrInputStream(expression);
            var lexer = new ExpressionLexer(input);
            var lexerErrorListener = new LexerErrorListener();
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(lexerErrorListener);

            var tokens = new CommonTokenStream(lexer);
            var parser = new ExpressionParser(tokens);
            var parserErrorListener = new SyntaxErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(parserErrorListener);
            var tree = parser.compileUnit();

            string? errorMessage = lexerErrorListener.ErrorMessage ?? parserErrorListener.ErrorMessage;
            if (errorMessage is not null)
            {
                throw new Exception(errorMessage);
            }

            var visitor = new EvaluatorVisitor(cell => EvaluateCell(cell, calculationPath));
            object result = visitor.Visit(tree);
            calculatedValues[address] = result;
            return result;
        }
        catch (Exception exception)
        {
            throw new Exception($"Клітинка {address}: {exception.Message}");
        }
        finally
        {
            calculationPath.Remove(address);
        }
    }

    private string GetDisplayedText(string address)
    {
        if (ModeSwitch.IsToggled && calculatedValues.TryGetValue(address, out object? value))
        {
            return FormatValue(value);
        }

        return cells.GetValueOrDefault(address, string.Empty);
    }

    private static string FormatValue(object value)
    {
        if (value is bool boolean)
        {
            return boolean ? "TRUE" : "FALSE";
        }

        return value is BigInteger number ? number.ToString() : value.ToString() ?? string.Empty;
    }

    private void RefreshCellTexts()
    {
        foreach (var item in cellEntries)
        {
            item.Value.Text = GetDisplayedText(item.Key);
            item.Value.IsReadOnly = ModeSwitch.IsToggled;
        }

        FormulaEntry.Text = cells.GetValueOrDefault(activeCell, string.Empty);
    }

    private void SaveVisibleEntries()
    {
        if (ModeSwitch.IsToggled)
        {
            return;
        }

        StoreAllEntries();
    }

    private void StoreAllEntries()
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
