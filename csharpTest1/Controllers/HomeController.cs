using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using CSharpTest1.Models;
using System.Drawing;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using SkiaSharp;
using ScottPlot;
using ScottPlot.Plottables;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<Employee>> GetEmployees()
    {
        var client = _httpClientFactory.CreateClient();
        var key = "vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
        var response = await client.GetAsync("https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=" + key);
        if (!response.IsSuccessStatusCode)
        {
            return new List<Employee>();
        }

        var json = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var records = JsonSerializer.Deserialize<List<EmployeeRecord>>(json, options);

        var filteredEmployees = records
            .GroupBy(a => a.EmployeeName)
            .Select(b => new Employee
            {
                EmployeeName = b.Key,
                TotalTimeWorked = b.Sum(c => Math.Abs((c.EndTimeUtc - c.StarTimeUtc).TotalHours))
            })
            .OrderByDescending(a => a.TotalTimeWorked)
            .ToList();

        return filteredEmployees;
    }

    public async Task<IActionResult> Index()
    {
        var result = await GetEmployees();
        return View(result);
    }

    [HttpGet("chart-image")]
    public async Task<IActionResult> GenerateChart()
    {
        var employees = await GetEmployees();

        var labels = employees.Select(a => a.EmployeeName).ToArray();
        var values = employees.Select(a => a.TotalTimeWorked).ToArray();

        var plot = new ScottPlot.Plot();
        var pie = plot.Add.Pie(values);
        plot.Legend.IsVisible = true;

        double total = pie.Slices.Select(x => x.Value).Sum();

        for (int i = 0; i < labels.Length; i++)
        {
            double percent = values[i] / total * 100;
            string legendLabel = $"{labels[i]} ({percent:F1}%)";

            var scatter = plot.Add.Scatter(new double[] { }, new double[] { });
            scatter.LegendText = legendLabel;
            scatter.LineWidth = 0;
            scatter.MarkerSize = 10;
        }

        double[] percentages = pie.Slices.Select(x => x.Value / total * 100).ToArray();

        for (int i = 0; i < pie.Slices.Count; i++)
        {
            pie.Slices[i].Label = $"{percentages[i]:0.0}%";
            pie.Slices[i].LabelFontSize = 20;
            pie.Slices[i].LabelBold = true;
            pie.Slices[i].LabelFontColor = Colors.Black.WithAlpha(.5);
        }

        plot.Axes.Frameless();
        plot.HideGrid();

        using var surface = SKSurface.Create(new SKImageInfo(600, 600));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        plot.Render(canvas, 600, 600);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream.ToArray(), "image/png");
    }
}

