using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// gh issue list --state all --limit 20000 --json number,createdAt,closed,url,closedAt,id,labels,milestone,state,title,assignees > issues.json
// ReadIssuesJson();

using (var context = new IssuesContext())
{
    PatchedFor(1, 0);
    PatchedFor(1, 1);
    PatchedFor(2, 0);
    PatchedFor(2, 1);
    PatchedFor(2, 2);
    PatchedFor(3, 0);
    PatchedFor(3, 1);
    PatchedFor(5, 0);
    PatchedFor(6, 0);
    // PatchedIssues();
    // BugsOpenAndClosed();
    // BugsOpened();
    // Bugs fixed in a patch release
    // Time to fix
    
    void BugsOpenAndClosed()
    {
        var issues = context.Issues
            .Include(i => i.Milestone)
            .Where(i => i.Labels.Any(l => l.Name == "type-bug") && (i.Milestone != null))
            .OrderBy(i => i.CreatedOn)
            .ToList();
        
        Console.WriteLine(issues.Count);
        
        var date = new DateTime(2014, 5, 1);
        while (date < DateTime.Today.AddDays(7))
        {
            var openCount = 0;
            var closedCount = 0;
            foreach (var issue in issues)
            {
                if (issue.CreatedOn <= date)
                {
                    if (issue.ClosedOn != null && issue.ClosedOn < date)
                    {
                        closedCount++;
                    }
                    else
                    {
                        openCount++;
                    }
                }
            }

            Console.WriteLine($"{date:d},{openCount},{closedCount}");
            date = date.AddDays(7);
        }
    }

    void BugsOpened()
    {
        var issues = context.Issues
            .Include(i => i.Milestone)
            .Where(i => i.Labels.Any(l => l.Name == "type-bug") && i.Milestone != null)
            .OrderBy(i => i.CreatedOn)
            .ToList();
        
        Console.WriteLine(issues.Count);

        var date = new DateTime(2014, 5, 1);
        while (date < DateTime.Today.AddDays(7))
        {
            var backTo = date.AddMonths(-1);
            var openCount = 0;
            foreach (var issue in issues)
            {
                if (issue.CreatedOn > backTo && issue.CreatedOn <= date)
                {
                    openCount++;
                }
            }

            Console.WriteLine($"{backTo:d},{openCount}");
            date = date.AddDays(7);
        }
    }
    
    void TooManyTypes()
    {
        var issues = context.Issues
            .Include(i => i.Labels)
            .Where(i => 
                !i.Labels.Any(l => l.Name == "closed-fixed") &&
                i.Labels.Count(l => l.Name.StartsWith("closed-")) > 1)
            //.OrderBy(i => i.CreatedOn)
            .ToList();

        foreach (var issue in issues)
        {
            //Console.WriteLine($"\"#{issue.Id}\", {string.Join(", ", issue.Labels.Select(l => l.Name))} \"{issue.Title}\"");
            Console.WriteLine(issue.Url);
        }
        
        Console.WriteLine();
        
        Console.WriteLine($"Total = {context.Issues.Local.Count}");

        Console.WriteLine();
    }

    void TimeToClose()
    {
        var issues = context.Issues
            .Where(i => i.State == IssueState.Fixed
                        && i.Labels.Any(l => l.Name == "type-bug"))
            .Select(i => new
            {
                i.Id,
                i.CreatedOn,
                i.ClosedOn,
                ClosedIn = (int)double.Round((i.ClosedOn.Value - i.CreatedOn).TotalDays),
                i.Title
            }).ToList();

        // for (var i = 0; i < 1600; i++)
        // {
        //     Console.WriteLine($"{i}, {issues.Count(issue => issue.ClosedIn == i)}");
        //     // Console.WriteLine($"\"{bucket - 5} to {bucket} days\", {issues.Count(i => i.ClosedIn < bucket && i.ClosedIn >= bucket - 5)}");
        // }
        for (int bucket = 7; bucket < 1600; bucket += 7)
        {
            Console.WriteLine($"{bucket/7}, {issues.Count(i => i.ClosedIn < bucket && i.ClosedIn >= bucket - 7)}");
            // Console.WriteLine($"\"{bucket - 5} to {bucket} days\", {issues.Count(i => i.ClosedIn < bucket && i.ClosedIn >= bucket - 5)}");
        }
    }
    
    void PatchedIssues()
    {
        var issues = context.Issues
            .Where(i => i.State == IssueState.Fixed
                        && i.Milestone.Patch != null
                        && i.Milestone.Patch != 0
                        && i.Labels.Any(l => l.Name == "type-bug"))
            .ToList();

        var date = new DateTime(2014, 5, 1);
        while (date < DateTime.Today.AddMonths(1))
        {
            var backTo = date.AddMonths(-1);
            var openCount = 0;
            foreach (var issue in issues)
            {
                if (issue.ClosedOn > backTo && issue.ClosedOn <= date)
                {
                    openCount++;
                }
            }

            Console.WriteLine($"{backTo:d},{openCount}");
            date = date.AddMonths(1);
        }

    }

    void PatchedFor(int major, int minor)
    {
        Console.WriteLine($"{major}.{minor}, {context.Issues
            .Count(i => i.State == IssueState.Fixed 
                        && i.Milestone!.Major == major
                        && i.Milestone!.Minor == minor
                        && i.Milestone!.Patch != 0)}");
        // Console.WriteLine($"{context.Issues
        //     .Count(i => i.State == IssueState.Fixed 
        //                 && i.Milestone!.Major == major
        //                 && i.Milestone!.Minor == minor
        //                 && i.Milestone!.Patch != 0)} Patched for {major}.{minor}");
    }

    void NoClosedLabel()
    {
        foreach (var issue in context.Issues
                     .Include(i => i.Milestone)
                     .Where(i => i.State == IssueState.Closed && i.Id >= 6641)
                     .OrderBy(i => i.Id))
        {
            Console.WriteLine($"gh issue reopen {issue.Id}");
            Console.WriteLine($"gh issue close {issue.Id} --reason \"not planned\"");
            //Console.WriteLine($"{issue.Milestone} #{issue.Id}: {issue.Title}");
        }

        Console.WriteLine();

        Console.WriteLine($"Total = {context.Issues.Local.Count}");

        Console.WriteLine();
    }

    void ListFixedIssues(string login, int release, string label, DateTime endDate)
    {
        Console.WriteLine($"'{label}' issues fixed by {login} in {release}.0.0:");

        Issue? previousIssue = null;
        var intervals = new List<TimeSpan>();
        
        foreach (var issue in context.Issues
                     .Include(i => i.Milestone)
                     .Include(i => i.Labels)
                     .Where(i => i.State == IssueState.Fixed
                                     && i.Milestone!.Major == release
                                     && i.Milestone!.Patch == 0
                                     && i.Assignees.Any(assignee => assignee.Login == login)
                                     && i.ClosedOn < endDate)
                     .OrderBy(i => i.ClosedOn))
        {
            if (previousIssue != null
                && issue.Labels.Any(l => l.Name == label))
            {
                intervals.Add(issue.ClosedOn!.Value - previousIssue.ClosedOn!.Value);
            
                Console.WriteLine(
                    $"From #{previousIssue.Id} to #{issue.Id} in {double.Round(intervals[^1].TotalDays)} days.");
            }

            previousIssue = issue;
        }

        var average = intervals.Select(i => i.TotalDays).Average();

        Console.WriteLine($"Average is {average}");
    }

    void ListIssues(IssueState state)
    {
        Console.WriteLine($"{state} issues:");
        foreach (var issue in context.Issues.Include(issue => issue.Milestone).Where(issue => issue.State == state))
        {
            var milestone = issue.Milestone != null ? $"({issue.Milestone}) " : "";
            Console.WriteLine($"  {milestone}{issue}");
        }
        Console.WriteLine();
    }
    
    void ListLabels()
    {
        Console.WriteLine("Labels:");
        foreach (var label in context.Labels)
        {
            Console.WriteLine($"  {label}");
        }
        Console.WriteLine();
    }
    
    void ListPeople()
    {
        Console.WriteLine($"People:");
        foreach (var person in context.People)
        {
            Console.WriteLine($"  {person}");
        }
        Console.WriteLine();
    }
    
    void ListMilestones()
    {
        Console.WriteLine($"Milestones:");
        foreach (var milestone in context.Milestones.OrderBy(m => m.Major).ThenBy(m => m.Minor).ThenBy(m => m.Patch))
        {
            Console.WriteLine($"  {milestone.Title}");
            // Console.WriteLine($"  {milestone.Major}.{milestone.Minor}.{milestone.Patch} ({milestone.Title})");
        }
        Console.WriteLine();
    }
}

void ReadIssuesJson()
{
    using var context = new IssuesContext();

    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
    
    using var json = File.OpenRead(@"C:\github\EFCoreIssues\EFCoreIssues\issues.json");
    using var document = JsonDocument.Parse(json);

    var root = document.RootElement;

    foreach (var issueJson in root.EnumerateArray())
    {
        var closedAtElement = issueJson.GetProperty("closedAt");
        var issue = new Issue(
            issueJson.GetProperty("number").GetInt32(),
            issueJson.GetProperty("createdAt").GetDateTime(),
            closedAtElement.ValueKind == JsonValueKind.Null ? null : closedAtElement.GetDateTime(),
            issueJson.GetProperty("title").GetString()!,
            new Uri(issueJson.GetProperty("url").GetString()!))
        {
            Milestone = FindOrCreateMilestone()
        };

        PopulateLabels(issue.Labels);
        PopulateAssignees(issue.Assignees);

        var isFixed = issue.Labels.Any(label => label.Name == "closed-fixed");
        issue.State = issueJson.GetProperty("state").GetString() == "CLOSED"
            ? isFixed ? IssueState.Fixed : IssueState.Closed
            : isFixed ? IssueState.Resolved : IssueState.Open;

        context.Add(issue);

        Console.WriteLine($"Importing issue {issue}");
        
        Milestone? FindOrCreateMilestone()
        {
            var milestoneObject = issueJson.GetProperty("milestone");
            
            var milestoneTitle = milestoneObject.ValueKind == JsonValueKind.Null
                ? null
                : milestoneObject.GetProperty("title").GetString();

            return milestoneTitle == null
                ? null
                : context.Milestones.Find(milestoneTitle) ?? Milestone.Parse(milestoneTitle);
        }

        void PopulateLabels(List<Label> labels)
        {
            foreach (var labelJson in issueJson.GetProperty("labels").EnumerateArray())
            {
                var name = labelJson.GetProperty("name").GetString()!;
                labels.Add(context.Labels.Find(name) ?? Label.Create(name));
            }
        }

        void PopulateAssignees(List<Person> assignees)
        {
            foreach (var assigneeJson in issueJson.GetProperty("assignees").EnumerateArray())
            {
                var login = assigneeJson.GetProperty("login").GetString()!;
                assignees.Add(context.People.Find(login)
                              ?? new Person(login, assigneeJson.GetProperty("name").GetString()!));
            }
        }
    }

    context.SaveChanges();
}

public class IssuesContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder
            //.LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            //.UseSqlServer(@"Data Source=(LocalDb)\MSSQLLocalDB;Database=EFIssues");
            .UseSqlite("Data Source = issues.db");

    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Person> People => Set<Person>();
}

public class Issue
{
    public Issue(int id, DateTime createdOn, DateTime? closedOn, string title, Uri url)
    {
        Id = id;
        CreatedOn = createdOn;
        ClosedOn = closedOn;
        Title = title;
        Url = url;
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; init; }
    public IssueState State { get; set; }
    public DateTime CreatedOn { get; init; }
    public DateTime? ClosedOn { get; set; }
    public string Title { get; init; }
    public Uri Url { get; init; }

    public Milestone? Milestone { get; set; }

    public List<Label> Labels { get; } = new();

    public List<Person> Assignees { get; } = new();

    public override string ToString() => $"#{Id}: {Title}";
}

public class Milestone
{
    public Milestone(string title, int? major, int? minor, int? patch)
    {
        Title = title;
        Major = major;
        Minor = minor;
        Patch = patch;
    } 

    public static Milestone Parse(string title)
    {
        var dot1 = title.IndexOf('.');
        var dot2 = title.IndexOf('.', dot1 + 1);
        if (dot1 == -1 || dot1 > 2 || dot2 == -1)
        {
            return new Milestone(title, null, null, null);
        }

        var patch = int.TryParse(title.Substring(dot2 + 1), out var value)
            ? value
            : 0;

        return new Milestone(
            title, 
            int.Parse(title.Substring(0, dot1)), 
            int.Parse(title.Substring(dot1 + 1, dot2 - dot1 - 1)),
            patch);
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Title { get; init; }
    
    public int? Major { get; init; }
    public int? Minor { get; init; }
    public int? Patch { get; init; }
    
    public HashSet<Issue> Issues { get; } = new(ReferenceEqualityComparer.Instance);

    public override string ToString() => Title;
}

public enum IssueState
{
    Open,
    Resolved,
    Fixed,
    Closed
}

public class Label
{
    public Label(string name, LabelCategory category)
    {
        Name = name;
        Category = category;
    }

    public static Label Create(string title)
    {
        return new(title,
            title.StartsWith("area-")
                ? LabelCategory.Area
                : title.StartsWith("type-")
                    ? LabelCategory.Type
                    : title.StartsWith("closed-")
                        ? LabelCategory.Closed
                        : LabelCategory.Other);
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Name { get; init; }
    
    public LabelCategory Category { get; init; }
    
    public List<Issue> Issues { get; } = new();

    public override string ToString() => Name;
}

public enum LabelCategory
{
    Other,
    Type,
    Area,
    Closed
}

public class Person
{
    public Person(string login, string name)
    {
        Login = login;
        Name = name;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Login { get; init; }

    public string Name { get; init; }

    public List<Issue> Issues { get; } = new();

    public override string ToString() => Name;
}
