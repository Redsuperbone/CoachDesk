using System.Globalization;
using System.IO;
using System.Text;
using CoachDesk.Desktop.Models.Nutrition;

namespace CoachDesk.Desktop.Services;

public sealed class MealPlanPdfExporter : IMealPlanPdfExporter
{
    public async Task ExportAsync(MealPlanDto plan, string adviseeName, string outputPath, CancellationToken cancellationToken = default)
    {
        var lines = BuildLines(plan, adviseeName);
        var document = CreatePdf(lines);
        await File.WriteAllBytesAsync(outputPath, document, cancellationToken);
    }

    private static IReadOnlyList<string> BuildLines(MealPlanDto plan, string adviseeName)
    {
        var lines = new List<string>
        {
            "MANRIQUEZ GYM - PLAN NUTRICIONAL",
            $"Asesorado: {adviseeName}",
            $"Plan: {plan.Name} | Vigencia: {plan.EffectiveDate:dd/MM/yyyy}",
            ""
        };

        foreach (var meal in plan.Meals)
        {
            lines.Add(GetMealName(meal.Type).ToUpperInvariant());
            foreach (var item in meal.Items)
            {
                lines.Add($"  {item.FoodName} - {item.Portion} | {item.Calories:0.#} kcal | P {item.ProteinGrams:0.#}g C {item.CarbohydratesGrams:0.#}g G {item.FatGrams:0.#}g");
            }
            lines.Add(string.Empty);
        }

        var allItems = plan.Meals.SelectMany(meal => meal.Items).ToArray();
        lines.Add($"TOTALES: {allItems.Sum(item => item.Calories):0.#} kcal | Proteina {allItems.Sum(item => item.ProteinGrams):0.#}g | Carbohidratos {allItems.Sum(item => item.CarbohydratesGrams):0.#}g | Grasas {allItems.Sum(item => item.FatGrams):0.#}g");
        if (!string.IsNullOrWhiteSpace(plan.Notes)) lines.Add($"Notas: {plan.Notes}");
        return lines;
    }

    private static byte[] CreatePdf(IReadOnlyList<string> lines)
    {
        const int linesPerPage = 42;
        var pages = lines.Chunk(linesPerPage).Select(CreatePageContent).ToArray();
        var pageCount = pages.Length == 0 ? 1 : pages.Length;
        if (pages.Length == 0) pages = [CreatePageContent([])];

        var objects = new List<string>();
        var pageObjectStart = 3;
        var contentObjectStart = pageObjectStart + pageCount;
        var fontObject = contentObjectStart + pageCount;
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(pageObjectStart, pageCount).Select(number => $"{number} 0 R"))}] /Count {pageCount} >>");

        for (var index = 0; index < pageCount; index++)
        {
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObject} 0 R >> >> /Contents {contentObjectStart + index} 0 R >>");
        }
        foreach (var page in pages)
        {
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(page)} >>\nstream\n{page}endstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        for (var index = 1; index < offsets.Count; index++) WriteAscii(stream, $"{offsets[index]:D10} 00000 n \n");
        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static string CreatePageContent(IEnumerable<string> lines)
    {
        var builder = new StringBuilder("BT\n/F1 10 Tf\n50 755 Td\n");
        foreach (var line in lines)
        {
            builder.Append('(').Append(Escape(line)).Append(") Tj\n0 -16 Td\n");
        }
        builder.Append("ET\n");
        return builder.ToString();
    }

    private static string Escape(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var ascii = new string(normalized.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && character <= 127).ToArray());
        return ascii.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string GetMealName(MealType type) => type switch
    {
        MealType.Breakfast => "Desayuno",
        MealType.Snack => "Colacion",
        MealType.Lunch => "Comida",
        MealType.Dinner => "Cena",
        _ => type.ToString()
    };

    private static void WriteAscii(Stream stream, string content)
    {
        var data = Encoding.ASCII.GetBytes(content);
        stream.Write(data, 0, data.Length);
    }
}
