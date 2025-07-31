---
mode: agent
tools: ['codebase', 'editFiles', 'fetch']
description: 'Create unit tests'
---

Create a unit test.  Do not modify the code under test.  The test should live in a matching namespace to the class under test.  It should have an identical name to the class under text except suffixed with the word Test e.g. ItemMakerServiceTest.  Use Use xUNit and Shouldly.  If mocking us Moq.  The test name should follow the format Given_When_Then for instance GivenZeroInventory_WhenGetInventoryRatio_ThenItIsZero.  Separate out sections into arrange, act, and assert.  Try to keep the arrange section small by using helper methods that primarily expose the values used in the assert section.  Helper methods should follow the naming convention MakeSomeObject.   Helper methods should be static.  Helper methods should provide default values and tests should not specify parameters that don't affect assertions.  For example:

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