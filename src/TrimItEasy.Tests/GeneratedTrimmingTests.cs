namespace TrimItEasy.Tests;

public class GeneratedTrimmingTests
{
    [Fact]
    public void Recursive_False()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
                new Phone { Type = " Home ", Number = " 987-654-3210 " }
            }
        };

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = person.HomeAddress,
            Employees = new List<Person> { person }
        };

        person.Employer = company;

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { Recursive = false });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
    }

    [Fact]
    public void Recursive_True()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
                new Phone { Type = " Home ", Number = " 987-654-3210 " }
            }
        };

        var company = new Company
        {
            Name = "  Initech  ",
            OfficeAddress = person.HomeAddress,
            Employees = new List<Person> { person }
        };

        person.Employer = company;

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { Recursive = true });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
    }

    [Fact]
    public void MaxDepth_Zero_TrimsOnlyTopLevelProperties()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
            }
        };

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { MaxDepth = 0 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
        Assert.Equal("  Springfield  ", person.HomeAddress.City);
        Assert.Equal("  USA  ", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("  Mobile  ", person.Phones[0].Type);
    }

    [Fact]
    public void MaxDepth_One_TrimsOneLevel()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
            Phones = new List<Phone>
            {
                new Phone { Type = "  Mobile  ", Number = " 123-456-7890 " },
            }
        };

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { MaxDepth = 1 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("  USA  ", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("  US  ", person.HomeAddress.CountryInfo.Code);
        Assert.Equal("Mobile", person.Phones[0].Type);
        Assert.Equal("123-456-7890", person.Phones[0].Number);
    }

    [Fact]
    public void MaxDepth_Two_TrimsTwoLevels()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
        };

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { MaxDepth = 2 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
    }

    [Fact]
    public void MaxDepth_Default_TrimsAllLevels()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
                CountryInfo = new Country
                {
                    Name = "  USA  ",
                    Code = "  US  "
                }
            },
        };

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions());

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("123 Elm St", person.HomeAddress.Street);
        Assert.Equal("Springfield", person.HomeAddress.City);
        Assert.Equal("USA", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("US", person.HomeAddress.CountryInfo.Code);
    }

    [Fact]
    public void MaxDepth_WithRecursiveFalse_DoesNotRecurse()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Elm St  ",
                City = "  Springfield  ",
            },
        };

        // Act
        person.GeneratedTrimStrings(new TrimmingOptions { Recursive = false, MaxDepth = 5 });

        // Assert
        Assert.Equal("John Doe", person.Name);
        Assert.Equal("  123 Elm St  ", person.HomeAddress.Street);
    }
}

public static partial class GeneratedTrimmingPersonExtensions
{
    [GeneratedTrimming]
    public static partial void GeneratedTrimStrings(this Person person, TrimmingOptions? options = null);
}
