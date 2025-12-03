using NetArchTest.Rules;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetSdrClientAppTests;

[TestFixture]
public class ArchitectureTests
{
    private readonly string _messagesNamespace = "NetSdrClientApp.Messages";
    private readonly string _networkingNamespace = "NetSdrClientApp.Networking";
    private readonly string _mainNamespace = "NetSdrClientApp";
    
    [Test]
    public void Messages_ShouldNotDependOnNetworking()
    {
        // Правило: Messages не повинен залежати від Networking
        var types = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_messagesNamespace);
            
        var result = types
            .ShouldNot()
            .HaveDependencyOn(_networkingNamespace)
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful, 
            $"Messages має заборонені залежності на Networking: {FormatViolations(result.FailingTypes)}");
    }
    
    [Test]
    public void MainProgram_ShouldNotReferenceTestProjects()
    {
        // Правило: Основний проєкт не повинен залежати від тестів
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_mainNamespace)
            .ShouldNot()
            .HaveDependencyOn("NetSdrClientAppTests")
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Основний проєкт має залежності на тести: {FormatViolations(result.FailingTypes)}");
    }
    
    [Test]
    public void Networking_Interfaces_ShouldBePublic()
    {
        // Правило: Інтерфейси в Networking повинні бути public
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_networkingNamespace)
            .And()
            .AreInterfaces()
            .Should()
            .BePublic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Інтерфейси в Networking мають бути public: {FormatViolations(result.FailingTypes)}");
    }
    
    [Test]
    public void AllClasses_InMessages_ShouldBeStatic()
    {
        // Правило: Всі класи в Messages повинні бути static
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_messagesNamespace)
            .And()
            .AreClasses()
            .Should()
            .BeStatic()
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Класи в Messages мають бути static: {FormatViolations(result.FailingTypes)}");
    }
    
    [Test]
    public void Networking_Wrappers_ShouldHaveCorrectNames()
    {
        // Правило: Класи-обгортки повинні мати "Wrapper" в назві
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(_networkingNamespace)
            .And()
            .AreClasses()
            .And()
            .ImplementInterface("ITcpClient")
            .Or()
            .ImplementInterface("IUdpClient")
            .Should()
            .HaveNameEndingWith("Wrapper")
            .GetResult();
        
        Assert.IsTrue(result.IsSuccessful,
            $"Wrapper класи мають мати 'Wrapper' в назві: {FormatViolations(result.FailingTypes)}");
    }
    
    private string FormatViolations(IEnumerable<Type> failingTypes)
    {
        return failingTypes != null && failingTypes.Any() 
            ? string.Join(", ", failingTypes.Select(t => t.Name))
            : "Немає порушень";
    }
}
