using Microsoft.AspNetCore.Mvc;
using MnemoToad.Learning.Api.Controllers;
using NUnit.Framework;

namespace MnemoToad.Learning.Tests.Controllers;

[TestFixture]
public class HealthControllerTests
{
    [Test]
    public void Get_ReturnsOkWithPassStatus()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var status = ok!.Value?.GetType().GetProperty("status")?.GetValue(ok.Value);
        Assert.That(status, Is.EqualTo("pass"));
    }
}
