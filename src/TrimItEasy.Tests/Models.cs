namespace TrimItEasy.Tests;

public class Person
{
    public string Name { get; set; }

    public Address HomeAddress { get; set; }

    public List<Phone> Phones { get; set; }

    public Company Employer { get; set; }
}

public class Address
{
    public string Street { get; set; }

    public string City { get; set; }

    public Country CountryInfo { get; set; }
}

public class Country
{
    public string Name { get; set; }

    public string Code { get; set; }
}

public class Phone
{
    public string Type { get; set; }

    public string Number { get; set; }
}

public class Company
{
    public string Name { get; set; }

    public Address OfficeAddress { get; set; }

    public List<Person> Employees { get; set; }
}
