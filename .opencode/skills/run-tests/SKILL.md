---
name: run-tests
description: Run the .NET test suite for the hexgame project using xUnit and FluentAssertions.
license: MIT
compatibility: opencode
metadata:
  version: "2.0"
  framework: "xunit"
---

# Run Tests

Run the .NET test suite.

## Usage

```
/run-tests [options]
```

## Commands

### Run all tests

```bash
dotnet test godot2/tests
```

### Verbose output

```bash
dotnet test godot2/tests --logger "console;verbosity=detailed"
```

### Run specific test file

```bash
dotnet test godot2/tests --filter "FullyQualifiedName~HexMapTutorial.Tests.HexCoordinatesTests"
```

### Run tests matching pattern

```bash
dotnet test godot2/tests --filter "DisplayName~River"
```

### Run specific test method

```bash
dotnet test godot2/tests --filter "FullyQualifiedName=HexMapTutorial.Tests.Generation.LandGeneratorTests.Generate_WithValidSeed_ProducesConsistentResults"
```

### Run with coverage

```bash
dotnet test godot2/tests --collect:"XPlat Code Coverage"
```

### Build only

```bash
dotnet build godot2/tests
```

## Test Structure

```
godot2/tests/
├── HexCoordinatesTests.cs
├── HexDirectionTests.cs
├── HexMetricsTests.cs
├── HexEdgeTypeTests.cs
├── HexCellRiverTests.cs
├── HexCellRoadTests.cs
├── EdgeVerticesTests.cs
├── HexGridChunkTests.cs
├── DistanceTests.cs
├── PathfindingTests.cs
├── Generation/
│   ├── ClimateGeneratorTests.cs
│   ├── FeatureGeneratorTests.cs
│   ├── GenerationConfigTests.cs
│   ├── GenerationMocks.cs
│   ├── LandGeneratorTests.cs
│   ├── MapGeneratorTests.cs
│   ├── RiverGeneratorTests.cs
│   └── RoadGeneratorTests.cs
├── Rendering/
│   ├── ChunkMathTests.cs
│   ├── PerformanceStatisticsTests.cs
│   └── RenderingConfigTests.cs
├── Source/
│   ├── HexCoordinates.Testable.cs
│   ├── HexMetrics.Testable.cs
│   └── HexEdgeType.Testable.cs
└── Mocks/
    └── GodotMocks.cs
```

## Namespace

All tests use namespace `HexMapTutorial.Tests`:

```csharp
namespace HexMapTutorial.Tests;

public class MyTests { }
```

Subdirectories add to namespace:
- `Generation/` → `HexMapTutorial.Tests.Generation`
- `Rendering/` → `HexMapTutorial.Tests.Rendering`

## Writing Tests

```csharp
using Xunit;
using FluentAssertions;

namespace HexMapTutorial.Tests;

public class FeatureTests
{
    [Fact]
    public void Method_Scenario_Expected()
    {
        // Arrange
        var input = new CellData { Elevation = 3 };

        // Act
        var result = SomeMethod(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 2, false)]
    public void Method_MultipleInputs_Expected(int a, int b, bool expected)
    {
        var result = SomeMethod(a, b);
        result.Should().Be(expected);
    }
}
```

## FluentAssertions Reference

```csharp
// Equality
result.Should().Be(5);
result.Should().BeEquivalentTo(expected);

// Numeric
value.Should().BeGreaterThan(0);
value.Should().BeInRange(1, 10);
value.Should().BeApproximately(3.14f, 0.01f);

// Collections
list.Should().HaveCount(5);
list.Should().Contain(item);
list.Should().BeEmpty();

// Boolean
result.Should().BeTrue();
result.Should().BeFalse();

// Null
obj.Should().BeNull();
obj.Should().NotBeNull();

// Exceptions
action.Should().Throw<ArgumentException>();
```

## Filter Examples

```bash
# By class name
--filter "FullyQualifiedName~HexMapTutorial.Tests.Generation.RiverGeneratorTests"

# By test name pattern
--filter "DisplayName~Elevation"

# By namespace
--filter "FullyQualifiedName~HexMapTutorial.Tests.Rendering"
```

## Checklist

- [ ] Tests compile: `dotnet build godot2/tests`
- [ ] All tests pass: `dotnet test godot2/tests`
- [ ] New code has test coverage
