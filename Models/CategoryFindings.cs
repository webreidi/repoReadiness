using System.Collections.Generic;

namespace RepoReadiness.Models;

public class CategoryFindings
{
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}