using PaveMetric;
using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using System.Xml.Serialization;

/// <summary>
/// The left palette shows only the error types ticked for the project, and the project
/// file remembers them. Types with recorded errors are always ticked on load.
/// </summary>
internal static class ProjectErrorTypeTests
{
    public static void Run()
    {
        CheckSerialization();

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { RunOnUiThread(); } catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        Console.WriteLine("Project error types: empty start, ticked-only palette and saved selection passed.");
    }

    private static void CheckSerialization()
    {
        var serializer = new XmlSerializer(typeof(Project));
        Project RoundTrip(Project project)
        {
            using var writer = new StringWriter();
            serializer.Serialize(writer, project);
            using var reader = new StringReader(writer.ToString());
            return (Project)serializer.Deserialize(reader)!;
        }

        Check(new Project().ErrorTypeCodes.Count == 0, "A new project must start with no types ticked.");

        var ticked = new Project();
        ticked.SetErrorTypeEnabled(14, true);
        ticked.SetErrorTypeEnabled(15, true);
        Check(string.Join(",", RoundTrip(ticked).ErrorTypeCodes) == "14,15", "The ticked types must be saved with the project.");

        // a project saved before the selection existed: only the types it uses come back
        var legacy = new Project();
        legacy.Photos.Add(new PavementPhoto { PhotoFileName = "a" });
        legacy.Photos[0].Errors.Add(new SurfaceError { ErrorCode = 22 });
        using var writer = new StringWriter();
        serializer.Serialize(writer, legacy);
        string xml = writer.ToString();
        int start = xml.IndexOf("<ErrorTypeCodes", StringComparison.Ordinal);
        if (start >= 0)
        {
            int end = xml.IndexOf("/>", start, StringComparison.Ordinal) + 2;
            int close = xml.IndexOf("</ErrorTypeCodes>", start, StringComparison.Ordinal);
            if (close >= 0) end = close + "</ErrorTypeCodes>".Length;
            xml = xml.Remove(start, end - start);
        }
        var loaded = (Project)serializer.Deserialize(new StringReader(xml))!;
        Check(loaded.ErrorTypeCodes.Count == 0, "A missing selection must load as none.");
        loaded.EnableUsedErrorTypes();
        Check(string.Join(",", loaded.ErrorTypeCodes) == "22", "Types used by recorded errors must be ticked on load.");
    }

    private static object Field(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static object? Invoke(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private static void RunOnUiThread()
    {
        // shown off-screen: Control.Visible only reports true for a displayed form
        using var main = new MainForm { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) };
        main.Show();
        Application.DoEvents();
        try
        {
            var group = (ErrorLayerGroup)Field(main, "errorLayerGroup");
            var project = (Project)Field(main, "_project");
            string Shown() => string.Join(",", group.ErrorLayers.Where(l => l.Visible).Select(l => l.ErrorCode).OrderBy(c => c));

            Check(Shown() == "", "A new project must not list any type in the palette.");

            project.SetErrorTypeEnabled(14, true);
            project.SetErrorTypeEnabled(10, true);
            Invoke(main, "ApplyProjectErrorTypes");
            Check(Shown() == "10,14", "Only the ticked types may be listed.");
            Check(group.ActiveLayer != null && group.ActiveLayer.Visible, "The active type must be a listed one.");

            project.SetErrorTypeEnabled(14, false);
            Invoke(main, "ApplyProjectErrorTypes");
            Check(Shown() == "10", "Unticking must remove the type from the palette.");
        }
        finally
        {
            ((Form)Field(main, "_errorTypeToolbox")).Dispose();
            main.Close();
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
