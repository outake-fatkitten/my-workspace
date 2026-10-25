# Click Counter

A minimal C# WinForms counter app. Click the button to increment the count, or
reset it back to zero. Built with .NET 8 (Windows Forms).

## Features

- One window with a live count display
- **Count +1** button: increments the counter by one
- **Reset** button: sets the counter back to zero

## Requirements

- .NET SDK 8.0 or newer (Windows)

## Build & Run

```bash
dotnet build ClickCounter.csproj
dotnet run --project ClickCounter.csproj
```

Or run the produced executable directly:

```bash
dotnet bin/Debug/net8.0-windows/ClickCounter.exe
```

## Project Layout

```
click-counter/
├── ClickCounter.csproj   # .NET 8 WinForms project file
├── Program.cs            # Application entry point
├── CounterForm.cs        # Main window: label + buttons
└── README.md
```
