namespace TrimItEasy.Tests;

public class NotTrimmedTests
{
    [Fact]
    public void TrimStrings_ShouldNotTrimPropertiesWithNotTrimmedAttribute()
    {
        // Arrange
        var person = new Person
        {
            Name = "  John Doe  ",
            HomeAddress = new Address
            {
                Street = "  123 Main St, Apt 4B  ",  // Should not be trimmed
                City = "  New York  ",
                CountryInfo = new Country
                {
                    Name = "  United States  ",
                    Code = "  US  "  // Should not be trimmed
                }
            },
            Phones = new List<Phone>
            {
                new Phone
                {
                    Type = "  Mobile  ",
                    Number = "  +1 (555) 123-4567  "  // Should not be trimmed
                }
            },
            Employer = new Company
            {
                Name = "  Acme Corp  ",
                OfficeAddress = new Address
                {
                    Street = "  456 Business Ave  ",  // Should not be trimmed
                    City = "  Boston  "
                },
                Employees = new List<Person>  // Should not be trimmed (though this is a collection)
                {
                    new Person { Name = "  Jane Smith  " }
                }
            }
        };

        // Act
        person.TrimStrings();

        // Assert
        Assert.Equal("John Doe", person.Name);
        
        // Address properties
        Assert.Equal("  123 Main St, Apt 4B  ", person.HomeAddress.Street);  // Not trimmed
        Assert.Equal("New York", person.HomeAddress.City);
        
        // Country properties
        Assert.Equal("United States", person.HomeAddress.CountryInfo.Name);
        Assert.Equal("  US  ", person.HomeAddress.CountryInfo.Code);  // Not trimmed
        
        // Phone properties
        Assert.Equal("Mobile", person.Phones[0].Type);
        Assert.Equal("  +1 (555) 123-4567  ", person.Phones[0].Number);  // Not trimmed
        
        // Company properties
        Assert.Equal("Acme Corp", person.Employer.Name);
        Assert.Equal("  456 Business Ave  ", person.Employer.OfficeAddress.Street);  // Not trimmed
        Assert.Equal("Boston", person.Employer.OfficeAddress.City);
        
        // Verify that the Employees collection is not trimmed (though this is a collection property)
        Assert.Equal("  Jane Smith  ", person.Employer.Employees[0].Name);
    }

    private class Person
    {
        public string Name { get; set; }

        public Address HomeAddress { get; set; }

        public List<Phone> Phones { get; set; }

        public Company Employer { get; set; }
    }

    private class Address
    {
        [NotTrimmed]
        public string Street { get; set; }  // Preserve street address formatting

        public string City { get; set; }

        public Country CountryInfo { get; set; }
    }

    private class Country
    {
        public string Name { get; set; }

        [NotTrimmed]
        public string Code { get; set; }  // Country codes should be exact
    }

    private class Phone
    {
        public string Type { get; set; }

        [NotTrimmed]
        public string Number { get; set; }  // Phone numbers should preserve exact formatting
    }

    private class Company
    {
        public string Name { get; set; }

        public Address OfficeAddress { get; set; }

        [NotTrimmed]
        public List<Person> Employees { get; set; }  // Example of NotTrimmed on a collection
    }
} 