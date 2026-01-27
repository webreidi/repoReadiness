namespace RepoReadiness.Assessors;

public interface IAssessor
{
    string CategoryName { get; }
    int MaxScore { get; }
    void Assess();
}