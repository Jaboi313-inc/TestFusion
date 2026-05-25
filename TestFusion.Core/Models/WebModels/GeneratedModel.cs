using TestFusion.Core.Models.TestResult;

namespace TestFusion.Core.Models.WebModels;

public class GeneratedModel
{
    public List<TestResultViewModel> Injectors { get; set; } = new();
    public List<TestModel> AllTests { get; set; } = new();
}