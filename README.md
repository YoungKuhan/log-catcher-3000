# log-catcher-3000
Library for automatic logging requests and responses for production testing 


## How to add this to your project
### In WebApi
```

var builder = WebApplication.CreateBuilder(args);

// Configure your logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddNLog(); 

var app = builder.Build();

// Pass depedency of it into library
LoggerFactoryInstance.Configure(app.Services.GetRequiredService<ILoggerFactory>());


```

### In console app

```

// Configure your logger
var loggerFactory = LoggerFactory.Create(builder =>
{
  builder.ClearProviders();
  builder.AddConsole();
  builder.AddNLog();
});

// Pass depedency of it into library
LoggerFactoryInstance.Configure(loggerFactory);


```
