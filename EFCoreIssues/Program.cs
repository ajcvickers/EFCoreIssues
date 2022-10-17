using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// gh issue list --state all --limit 20000 --json number,createdAt,closed,url,closedAt,id,labels,milestone,state,title,assignees > issues.json
ReadIssuesJson();

using (var context = new IssuesContext())
{
    RateOfClose();
    // ListMilestones();
    
    // Console.WriteLine($"Issue count: {context.Issues.Count(issue => issue.State == IssueState.Fixed)}");
    // ListPeople();
    // ListLabels();
    // ListMilestones();
    
    // ListFixedIssues("smitpatel", 6, "type-enhancement", new DateTime(2021, 11, 1));
    // ListFixedIssues("ajcvickers", 6, "type-enhancement", new DateTime(2021, 11, 1));
    // ListFixedIssues("roji", 6, "type-enhancement", new DateTime(2021, 11, 1));
    // ListFixedIssues("bricelam", 6, "type-enhancement", new DateTime(2021, 11, 1));
    // ListFixedIssues("maumar", 6, "type-enhancement", new DateTime(2021, 11, 1));
    // ListFixedIssues("smitpatel", 6, "type-bug", new DateTime(2021, 11, 1));
    // ListFixedIssues("ajcvickers", 6, "type-bug", new DateTime(2021, 11, 1));
    // ListFixedIssues("roji", 6, "type-bug", new DateTime(2021, 11, 1));
    // ListFixedIssues("bricelam", 6, "type-bug", new DateTime(2021, 11, 1));
    // ListFixedIssues("maumar", 6, "type-bug", new DateTime(2021, 11, 1));

    void RateOfClose()
    {
        var issues = context.Issues
            .Where(i => 
                // i.State == IssueState.Fixed
                //         && 
                        i.Labels.Any(l => l.Name == "type-bug"))
            .OrderBy(i => i.CreatedOn)
            .ToList();

        // var i = 0;
        // var date = new DateOnly(2014, 1, 1);
        // while (date < new DateOnly(2022, 10, 1))
        // {
        //     while (DateOnly.FromDateTime(issues[i++].ClosedOn!.Value) < date)
        //     {
        //         
        //     }
        //     date = date.AddMonths(1);
        // }

        var count = 0;
        var date = new DateTime(2014, 5, 1);
        foreach (var issue in issues)
        {
            if (issue.CreatedOn < date)
            {
                count++;
            }
            else
            {
                Console.WriteLine($"{date.AddMonths(-1):d},{count}");
                date = date.AddMonths(1);
                count = 0;
            }
            
            // Console.WriteLine($"gh issue reopen {issue.Id}");
            // Console.WriteLine($"gh issue close {issue.Id} --reason \"not planned\"");
            // Console.WriteLine($"\"#{issue.Id}\", {issue.CreatedOn:d}, {issue.ClosedOn:d}, \"{issue.Title}\"");
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

        for (int bucket = 7; bucket < 1600; bucket += 7)
        {
            Console.WriteLine($"{bucket/7}, {issues.Count(i => i.ClosedIn < bucket && i.ClosedIn >= bucket - 7)}");
            // Console.WriteLine($"\"{bucket - 5} to {bucket} days\", {issues.Count(i => i.ClosedIn < bucket && i.ClosedIn >= bucket - 5)}");
        }
        // foreach (var issue in issues.OrderBy(i => i.ClosedIn))
        // {
        //     // Console.WriteLine($"gh issue reopen {issue.Id}");
        //     // Console.WriteLine($"gh issue close {issue.Id} --reason \"not planned\"");
        //     Console.WriteLine($"{issue.Id}, {issue.CreatedOn:d}, {issue.ClosedOn:d}, {issue.ClosedIn}, \"{issue.Title}\"");
        // }
        //
        // Console.WriteLine();
        //
        // Console.WriteLine($"Total = {context.Issues.Local.Count}");

        Console.WriteLine();
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
        
        // Console.WriteLine();
    }

    // ListIssues(IssueState.Open);
    // ListIssues(IssueState.Resolved);
    // ListIssues(IssueState.Fixed);
    // ListIssues(IssueState.Closed);

    // void ListIssues(IssueState state)
    // {
    //     Console.WriteLine($"{state} issues:");
    //     foreach (var issue in context.Issues.Include(issue => issue.Milestone).Where(issue => issue.State == state))
    //     {
    //         var milestone = issue.Milestone != null ? $"({issue.Milestone}) " : "";
    //         Console.WriteLine($"  {milestone}{issue}");
    //     }
    //     Console.WriteLine();
    // }
    //
    // void ListLabels()
    // {
    //     Console.WriteLine("Labels:");
    //     foreach (var label in context.Labels)
    //     {
    //         Console.WriteLine($"  {label}");
    //     }
    //     Console.WriteLine();
    // }
    //
    // void ListPeople()
    // {
    //     Console.WriteLine($"People:");
    //     foreach (var person in context.People)
    //     {
    //         Console.WriteLine($"  {person}");
    //     }
    //     Console.WriteLine();
    // }
    
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
    public DateTime? ClosedOn { get; init; }
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
