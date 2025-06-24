# GitHub Copilot Instructions

These instructions define how GitHub Copilot should assist with this project. The goal is to ensure consistent, high-quality code generation aligned with our conventions, stack, and best practices.

## 🧠 Context

- **Project Type**: Console App
- **Language**: C#
- **Framework / Libraries**: .NET 9 / xUnit
- **Architecture**: Clean Architecture

## 🔧 General Guidelines

- Use C#-idiomatic patterns and follow .NET coding conventions.
- Use primary constructors when possible
- Use nullable reference types (`#nullable enable`) and async/await.
- Format using `dotnet format` or IDE auto-formatting tools.
- Prioritize readability, testability, and SOLID principles.

## Testing

- Use xUnit
- Use Shouldly for assertions

### Naming conventions

Unit tests should names should consist of three parts, using the following format Given[Arrange]_When[Act]_Then[Assert] 
* Given - should append the starting conditions and any parameter conditions 
* When - should append the method name being tested
* Then - should append the expected result

Example where parameter is not included: 
Item ItemRepository.GetById(int id)
GivenItemExists_WhenGetById_ThenItIsReturned

Example where the parameter is included:

`List<Item> ItemRepository.Filter(CategoryType type)`

`GivenItemMatchesCategory_WhenGetById_ThenItIsReturned`

## Helper methods
* In the arrange, when instantiating objects, prefer to create helper methods that take optional parameters
* Helper methods should follow the naming convention MakeSomeObject
* Helper methods should be static
* Accept the default values of parameters if the parameters do not affect the assertions

### Example

Don't do this:

```
public class Item() {
    public int Id { get; set; }
    public string Name { get; set; }
    public Category Category { get; set; }
}

[Fact]
public void GivenItemExists_WhenGetById_ThenItIsReturned()
{
    // Arrange
    var item = new Item()
    {
        Id = 1,
        Name = "test",
        Category = Category.Sports 
    }
    var repo = new ItemRepository();

    // Act
    var result = repo.GetById(1);

    // Assert
    result.ShouldNotBeNull();
}
```

Instead create a helper method to create item:

```
[Fact]
public void GivenItemExists_WhenGetById_ThenItIsReturned()
{
// Arrange
    var item = MakeItem(id: 1);
    var repo = new ItemRepository();
    // Act
    var result = repo.GetById(1);
    // Assert
    result.ShouldNotBeNull();
}

private static Item MakeItem(int id = 0, string name = "Test", Category categroy = Category.Sports)
{
    return new Item()
    {
        Id = id,
        Name = name,
        Category = category
    };
}
```
