﻿
namespace E_Commerce.DAL.DomainModels;
public class ApplicationUser:IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? ActivationToken { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public string? ConfirmEmailCode { get; set; } = string.Empty;

    //> navigation properties in the child class(Customer)
    
}