# TrimItEasy

A simple and efficient .NET library for automatically trimming string properties in complex objects.

## Installation

Install the package from NuGet:

```bash
dotnet add package TrimItEasy
```

Or using the NuGet Package Manager in Visual Studio:
```
Install-Package TrimItEasy
```

## Usage

### Basic Usage

Simply call the `TrimStrings()` extension method on any complex object:

```csharp
using TrimItEasy;

public class Person
{
    public string Name { get; set; }

    public string Email { get; set; }

    public Address HomeAddress { get; set; }
}

var person = new Person 
{
    Name = "  John Doe  ",
    Email = "  john@example.com  ",
    HomeAddress = new Address 
    {
        Street = "  123 Main St  ",
        City = "  New York  "
    }
};

// Trim all string properties
person.TrimStrings();
```

### Skip Trimming Specific Properties

Use the `[NotTrimmed]` attribute to prevent trimming of specific properties:

```csharp
public class Address
{
    [NotTrimmed]
    public string Street { get; set; }  // This property will not be trimmed
    
    public string City { get; set; }    // This property will be trimmed
}

var address = new Address 
{
    Street = "  123 Main St, Apt 4B  ",  // Will remain unchanged
    City = "  New York  "                 // Will be trimmed to "New York"
};

address.TrimStrings();
```

### Recursive Trimming

By default, the library recursively trims all string properties in nested objects. You can disable this behavior:

```csharp
// Only trim top-level string properties
person.TrimStrings(recursive: false);
```

### Supported Types

The library works with:
- Complex objects with string properties
- Collections (Lists, Arrays, etc.)
- Nested objects
- Properties marked with `[NotTrimmed]` will be skipped

### Limitations

The library will throw an `ArgumentException` if you try to use it on:
- String types (use `string.Trim()` instead)
- Primitive types
- Enum types

## Examples

### Working with Collections

```csharp
public class Company
{
    public string Name { get; set; }

    public List<Employee> Employees { get; set; }
}

public class Employee
{
    public string Name { get; set; }

    [NotTrimmed]
    public string EmployeeId { get; set; }
}

var company = new Company
{
    Name = "  Acme Corp  ",
    Employees = new List<Employee>
    {
        new Employee { Name = "  John Doe  ", EmployeeId = "  E001  " },
        new Employee { Name = "  Jane Smith  ", EmployeeId = "  E002  " }
    }
};

company.TrimStrings();
// Result:
// - company.Name = "Acme Corp"
// - company.Employees[0].Name = "John Doe"
// - company.Employees[0].EmployeeId = "  E001  " (not trimmed due to [NotTrimmed])
```

### Working with Nested Objects

```csharp
public class Order
{
    public string OrderNumber { get; set; }

    public Customer Customer { get; set; }

    public List<OrderItem> Items { get; set; }
}

public class Customer
{
    public string Name { get; set; }

    [NotTrimmed]
    public string PhoneNumber { get; set; }
}

var order = new Order
{
    OrderNumber = "  ORD-001  ",
    Customer = new Customer
    {
        Name = "  John Doe  ",
        PhoneNumber = "  +1 (555) 123-4567  "
    },
    Items = new List<OrderItem>
    {
        new OrderItem { Description = "  Product A  " }
    }
};

order.TrimStrings();
// All string properties will be trimmed except Customer.PhoneNumber
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details. 