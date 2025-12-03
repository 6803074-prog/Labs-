using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;

namespace NetSdrClientAppTests;

[TestFixture]
public class ArchitectureTests
{
    [Test]
    public void Messages_ShouldNotDependOnNetworking()
    {
        // Правило 1: Messages не повинен залежати від Networking
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace("NetSdrClientApp.Messages")
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientApp.Networking")
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful, 
            $"Messages має заборонені залежності на Networking. Порушення: {GetFailingTypeNames(result)}");
    }
    
    [Test]
    public void MainProgram_ShouldNotReferenceTestProjects()
    {
        // Правило 2: Основний проєкт не повинен залежати від тестів
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace("NetSdrClientApp")
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientAppTests")
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Основний проєкт має залежності на тести. Порушення: {GetFailingTypeNames(result)}");
    }
    
    [Test]
    public void AllClasses_InMessages_ShouldBeStatic()
    {
        // Правило 3: Всі класи в Messages повинні бути static (оскільки там тільки хелпери)
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace("NetSdrClientApp.Messages")
            .And()
            .AreClasses()
            .Should()
            .BeStatic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Класи в Messages мають бути static. Порушення: {GetFailingTypeNames(result)}");
    }
    
    [Test]
    public void Networking_Interfaces_ShouldBePublic()
    {
        // Правило 4: Інтерфейси в Networking повинні бути public
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace("NetSdrClientApp.Networking")
            .And()
            .AreInterfaces()
            .Should()
            .BePublic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Інтерфейси в Networking мають бути public. Порушення: {GetFailingTypeNames(result)}");
    }
    
    // Допоміжний метод для отримання назв типів, що порушують правила
    private string GetFailingTypeNames(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "Немає порушень";
        
        return string.Join(", ", result.FailingTypes.Select(t => t.Name));
    }
}
