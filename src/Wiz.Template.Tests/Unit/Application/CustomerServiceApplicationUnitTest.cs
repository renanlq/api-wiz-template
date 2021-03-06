﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wiz.Template.API.Controllers;
using Wiz.Template.API.Services.Interfaces;
using Wiz.Template.API.ViewModels.Customer;
using Wiz.Template.Tests.Mocks.ViewModels;
using Xunit;

namespace Wiz.Template.Tests.Unit.Application
{
    public class CustomerServiceApplicationUnitTest
    {
        private readonly Mock<ICustomerService> _customerServiceMock;

        public CustomerServiceApplicationUnitTest()
        {
            _customerServiceMock = new Mock<ICustomerService>();
        }

        [Fact]
        public async Task GetAll_SucessTestAsync()
        {
            _customerServiceMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(CustomerViewModelMock.GetCustomersAddress());

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.List();

            var actionResult = Assert.IsType<OkObjectResult>(customerService.Result);
            var actionValue = Assert.IsAssignableFrom<IEnumerable<CustomerAddressViewModel>>(actionResult.Value);

            Assert.NotNull(actionResult);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
        }

        [Fact]
        public async Task GetById_SucessTestAsync()
        {
            var id = 1;
            var customerId = CustomerViewModelMock.GetCustomerId(id);

            _customerServiceMock.Setup(x => x.GetAddressByIdAsync(customerId))
                .ReturnsAsync(CustomerViewModelMock.GetCustomerAddress());

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Get(customerId);

            var actionResult = Assert.IsType<OkObjectResult>(customerService.Result);
            var actionValue = Assert.IsType<CustomerAddressViewModel>(actionResult.Value);

            Assert.NotNull(actionResult);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
        }

        [Fact]
        public async Task GetByName_SucessTestAsync()
        {
            var name = "Zier Zuveiku";
            var customerName = CustomerViewModelMock.GetCustomerName(name);

            _customerServiceMock.Setup(x => x.GetAddressByNameAsync(customerName))
                .ReturnsAsync(CustomerViewModelMock.GetCustomerAddress());

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Get(customerName);

            var actionResult = Assert.IsType<OkObjectResult>(customerService.Result);
            var actionValue = Assert.IsType<CustomerAddressViewModel>(actionResult.Value);

            Assert.NotNull(actionResult);
            Assert.Equal(StatusCodes.Status200OK, actionResult.StatusCode);
        }

        [Fact]
        public void Post_SucessTestAsync()
        {
            var customer = CustomerViewModelMock.GetCustomer();

            _customerServiceMock.Setup(x => x.Add(customer))
                .Returns(CustomerViewModelMock.GetCustomer());

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = customerController.Post(customer);

            var actionResult = Assert.IsType<CreatedResult>(customerService.Result);
            var actionValue = Assert.IsType<CustomerViewModel>(actionResult.Value);

            Assert.NotNull(actionValue);
            Assert.Equal(StatusCodes.Status201Created, actionResult.StatusCode);
        }

        [Fact]
        public void Post_FailTestAsync()
        {
            CustomerViewModel customer = null;

            _customerServiceMock.Setup(x => x.Add(customer))
                .Returns(CustomerViewModelMock.GetCustomer());

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = customerController.Post(customer);

            var actionResult = Assert.IsType<NotFoundResult>(customerService.Result);

            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
        }

        [Fact]
        public async Task Put_BadRequestTestAsync()
        {
            var id = 1;
            CustomerViewModel customer = null;

            _customerServiceMock.Setup(x => x.Update(customer));

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Put(id, customer);

            var actionResult = Assert.IsType<BadRequestResult>(customerService);

            Assert.Equal(StatusCodes.Status400BadRequest, actionResult.StatusCode);
        }

        [Fact]
        public async Task Put_NotFoundTestAsync()
        {
            var id = 1;
            var customer = CustomerViewModelMock.GetCustomer();

            _customerServiceMock.Setup(x => x.Update(customer));

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Put(id, customer);

            var actionResult = Assert.IsType<NotFoundResult>(customerService);

            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
        }

        [Fact]
        public async Task Delete_SucessTestAsync()
        {
            var id = 1;
            var customerId = CustomerViewModelMock.GetCustomerId(id);
            var customer = CustomerViewModelMock.GetCustomer();

            _customerServiceMock.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _customerServiceMock.Setup(x => x.Remove(customer));

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Delete(customerId);

            var actionResult = Assert.IsType<NoContentResult>(customerService);

            Assert.Equal(StatusCodes.Status204NoContent, actionResult.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFoundTestAsync()
        {
            var id = 1;
            var customerId = CustomerViewModelMock.GetCustomerId(id);
            var customer = CustomerViewModelMock.GetCustomer();

            var customerController = new CustomerController(_customerServiceMock.Object);
            var customerService = await customerController.Delete(customerId);

            var actionResult = Assert.IsType<NotFoundResult>(customerService);

            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
        }
    }
}
