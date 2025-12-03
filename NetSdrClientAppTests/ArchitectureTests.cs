using NetArchTest.Rules;
using NUnit.Framework;
using System;
using System.Linq;

namespace NetSdrClientAppTests;

[TestFixture]
public class ArchitectureTests
{
    // Визначаємо неймспейси з твоєї структури
    private readonly string _messagesNamespace = "NetSdrClientApp.Messages";
    private readonly string _networkingNamespace = "NetSdrClientApp.Networking";
    private readonly string _mainNamespace = "NetSdrClientApp";
    
    [Test]
    public void Messages_ShouldNotDependOnNetworking()
    {
        // Правило: модуль Messages не повинен залежати від Networking
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_messagesNamespace)
            .ShouldNot()
            .HaveDependencyOn(_networkingNamespace)
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful, 
            $"Messages має заборонені залежності на Networking: {FormatViolations(result.ViolatingTypes)}");
    }
    
    [Test]
    public void Networking_Interfaces_ShouldBePublic()
    {
        // Правило: всі інтерфейси в Networking повинні бути public
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_networkingNamespace)
            .And()
            .AreInterfaces()
            .Should()
            .BePublic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Інтерфейси в Networking мають бути public: {FormatViolations(result.ViolatingTypes)}");
    }
    
    [Test]
    public void MessageHelpers_ShouldBeStatic()
    {
        // Правило: класи-хелпери в Messages повинні бути static
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_messagesNamespace)
            .And()
            .HaveNameEndingWith("Helper")
            .Should()
            .BeStatic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Хелпери в Messages мають бути static: {FormatViolations(result.ViolatingTypes)}");
    }
    
    [Test]
    public void Wrappers_ShouldImplementInterfaces()
    {
        // Правило: класи-обгортки повинні реалізовувати відповідні інтерфейси
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_networkingNamespace)
            .And()
            .HaveNameEndingWith("Wrapper")
            .Should()
            .ImplementInterface(typeof(NetSdrClientApp.Networking.ITcpClient))
            .OrShould()
            .ImplementInterface(typeof(NetSdrClientApp.Networking.IUdpClient))
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Wrapper класи мають реалізовувати інтерфейси: {FormatViolations(result.ViolatingTypes)}");
    }
    
    [Test]
    public void MainProgram_ShouldNotReferenceTestProjects()
    {
        // Правило: основний проєкт не повинен залежати від тестів
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_mainNamespace)
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientAppTests")
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Основний проєкт має залежності на тести: {FormatViolations(result.ViolatingTypes)}");
    }
    
    [Test]
    public void AllClasses_InNetworking_ShouldBeSealed()
    {
        // Правило: всі класи в Networking (крім абстрактних) мають бути sealed
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_networkingNamespace)
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Класи в Networking мають бути sealed: {FormatViolations(result.ViolatingTypes)}");
    }
    
    // Допоміжний метод
    private string FormatViolations(System.Collections.Generic.IEnumerable<Type> violatingTypes)
    {
        return violatingTypes.Any() 
            ? string.Join(", ", violatingTypes.Select(t => t.Name))
            : "Немає порушень";
    }
}
