﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.Events;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using IdentityServer4.Extensions;
using System.Security.Principal;
using System.Security.Claims;
using IdentityModel;
using System.Linq;
using System;
using System.Collections.Generic;
using Skoruba.IdentityServer4.Admin.EntityFramework.Entities.Identity;
using Skoruba.IdentityServer4.Admin.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Services;
using NPOI.SS.UserModel;
using Skoruba.IdentityServer4.Admin.EntityFramework.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Skoruba.IdentityServer4.AspNetIdentity.Quickstart.Account;
using System.Text.RegularExpressions;

namespace IdentityServer4.Quickstart.UI
{
    [SecurityHeaders]
    public class AccountController : Controller
    {
        public const string AdministrationRole = "SkorubaIdentityAdminAdministrator";


        private readonly UserManager<UserIdentity> _userManager;
        private readonly SignInManager<UserIdentity> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly AdminDbContext _adminDbContext;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<UserIdentity> userManager,
            SignInManager<UserIdentity> signInManager,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            AdminDbContext adminDbContext,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _adminDbContext = adminDbContext ?? throw new NotImplementedException(nameof(adminDbContext));
            _logger = logger;
        }

        [Authorize(Roles = AdministrationRole)]
        public IActionResult ImportUser()
        {
            return StatusCode(StatusCodes.Status423Locked);
            //return View();
        }

        //[Authorize(Roles = AdministrationRole)]
        //[HttpPost]
        //public async Task<IActionResult> ImportUser(IFormFile formFile)
        //{
        //    using (MemoryStream memoryStream = new MemoryStream())
        //    {
        //        await formFile.CopyToAsync(memoryStream);
        //        IWorkbook workbook = WorkbookFactory.Create(memoryStream);
        //        ISheet sheet = workbook.GetSheetAt(0);
        //        IRow row = sheet.GetRow(0);
        //        if (row.GetCell(0).ToString() != "工号" || row.GetCell(1).ToString() != "姓名" || row.GetCell(2).ToString() != "身份证号")
        //        {
        //            return Content("模板与预期不一致");
        //        }
        //        List<Employee> employeeList = new List<Employee>();
        //        for (int i = 1; i <= sheet.LastRowNum; i++)
        //        {
        //            var tmpRow = sheet.GetRow(i);
        //            employeeList.Add(new Employee { GH_工号 = tmpRow.GetCell(0).ToString(), XM_姓名 = tmpRow.GetCell(1).ToString(), SFZH_身份证号 = tmpRow.GetCell(2).ToString() });
        //        }

        //        if (employeeList.Count == 0)
        //        {
        //            return Content("无数据");
        //        }

        //        var exisitedGHList = await _adminDbContext.Employees.Select(x => x.GH_工号).ToListAsync();

        //        var willAddEmploryees = employeeList.Where(x => !exisitedGHList.Contains(x.GH_工号));

        //        await _adminDbContext.Employees.AddRangeAsync(willAddEmploryees);

        //        try
        //        {
        //            var saved = await _adminDbContext.SaveChangesAsync();
        //            var resp = string.Join(';', exisitedGHList);

        //            return Content(string.Format("导入完成，成功{0}，失败{1}。{2}", saved, exisitedGHList.Count, resp));

        //        }
        //        catch (DbUpdateException e)
        //        {
        //            _logger.LogError(e, "ImportUser");
        //            return StatusCode(StatusCodes.Status505HttpVersionNotsupported);
        //        }

        //    }
        //}

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return await ExternalLogin(vm.ExternalLoginScheme, returnUrl);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            if (button != "login")
            {
                // the user clicked the "cancel" button
                var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.GrantConsentAsync(context, ConsentResponse.Denied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    var employee = await _adminDbContext.Employees.FirstOrDefaultAsync(x => x.GH_工号 == model.Username.ToLower());
                    if (employee == null)
                    {
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "不存在的工号"));

                        ModelState.AddModelError("", "你输入的工号未录入，请联系管理员");
                    }
                    else
                    {
                        ModelState.AddModelError("", "你输入的工号还没激活，点击下方链接注册激活");
                    }
                }
                else
                {
                    var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, AccountOptions.AllowRememberLogin && model.RememberLogin, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id.ToString(), user.UserName));

                        // make sure the returnUrl is still valid, and if so redirect back to authorize endpoint or a local page
                        // the IsLocalUrl check is only necessary if you want to support additional local pages, otherwise IsValidReturnUrl is more strict
                        if (_interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                        {
                            return Redirect(model.ReturnUrl);
                        }

                        return Redirect("~/");
                    }

                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials"));

                    ModelState.AddModelError("", AccountOptions.InvalidCredentialsErrorMessage);
                }
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            if (AccountOptions.WindowsAuthenticationSchemeName == provider)
            {
                // windows authentication needs special handling
                return await ProcessWindowsLoginAsync(returnUrl);
            }
            else
            {
                // start challenge and roundtrip the return URL and 
                var props = new AuthenticationProperties()
                {
                    RedirectUri = Url.Action("ExternalLoginCallback"),
                    Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", provider },
                    }
                };
                return Challenge(props, provider);
            }
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            // read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }

            // lookup our user and external provider info
            var (user, provider, providerUserId, claims) = await FindUserFromExternalProviderAsync(result);
            if (user == null)
            {
                // this might be where you might initiate a custom workflow for user registration
                // in this sample we don't show how that would be done, as our sample implementation
                // simply auto-provisions new external user
                user = await AutoProvisionUserAsync(provider, providerUserId, claims);
            }

            // this allows us to collect any additonal claims or properties
            // for the specific prtotocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additionalLocalClaims = new List<Claim>();
            var localSignInProps = new AuthenticationProperties();
            ProcessLoginCallbackForOidc(result, additionalLocalClaims, localSignInProps);
            ProcessLoginCallbackForWsFed(result, additionalLocalClaims, localSignInProps);
            ProcessLoginCallbackForSaml2p(result, additionalLocalClaims, localSignInProps);

            // issue authentication cookie for user
            // we must issue the cookie maually, and can't use the SignInManager because
            // it doesn't expose an API to issue additional claims from the login workflow
            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            additionalLocalClaims.AddRange(principal.Claims);
            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value ?? user.Id.ToString();
            await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.Id.ToString(), name));
            await HttpContext.SignInAsync(user.Id.ToString(), name, provider, localSignInProps, additionalLocalClaims.ToArray());

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // validate return URL and redirect back to authorization endpoint or a local page
            var returnUrl = result.Properties.Items["returnUrl"];
            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await _signInManager.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        //启用注册，工号与身份证核对
        public IActionResult Register(string returnUrl)
        {
            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Skoruba.IdentityServer4.AspNetIdentity.Quickstart.Account.RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.UserName.ToLower();

                var userExisted = await _userManager.FindByNameAsync(model.UserName);
                if (userExisted != null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "此工号已被激活过，请直接登录");
                    return View(model);
                }

                var employ = await _adminDbContext.Employees.FirstOrDefaultAsync(x => x.GH_工号 == model.UserName);
                if (employ == null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "此工号未登记，请确认后联系管理员");
                    return View(model);
                }
                else
                {
                    if (AccountOptions.RegisterCheckSFZ)
                    {
                        if (string.IsNullOrWhiteSpace(employ.SFZH_身份证号))
                        {
                            ModelState.AddModelError(nameof(model.UserName), "此工号未登记身份证信息，请联系管理员");
                            return View(model);
                        }
                        else
                        {
                            var len = employ.SFZH_身份证号.Length;
                            if (len < 4) //其实应该是<15就是错误的身份证号
                            {
                                ModelState.AddModelError(nameof(model.UserName), "此工号登记的身份证信息有误，请联系管理员");
                                return View(model);
                            }
                            else if (employ.SFZH_身份证号.Substring(len - 4).ToLower() != model.OldPassword.ToLower())
                            {
                                ModelState.AddModelError(nameof(model.OldPassword), "初始密码错误");
                                return View(model);
                            }
                        }
                    }

                    //正式注册
                    var user = new UserIdentity { UserName = model.UserName, Email = model.Email };
                    var ir = await _userManager.CreateAsync(user, model.Password);
                    if (ir.Succeeded)
                    {
                        return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
                    }
                    else
                    {
                        return StatusCode(500, string.Join(',', ir.Errors.Select(x => $"{x.Code}:{x.Description}").DefaultIfEmpty()));
                    }

                }
            }
            else
            {
                return View(model);
            }
        }


        //修改密码
        public IActionResult ResetPassword(string returnUrl)
        {
            return View(new ResetPasswordViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(Skoruba.IdentityServer4.AspNetIdentity.Quickstart.Account.ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.UserName.ToLower();

                var userExisted = await _userManager.FindByNameAsync(model.UserName);
                if (userExisted == null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "账号不存在");
                    return View(model);
                }
                else if (userExisted.Email != model.Email)
                {
                    if (string.IsNullOrWhiteSpace(userExisted.Email))
                    {
                        ModelState.AddModelError(nameof(model.Email), "激活时填写的Email地址有误，请联系管理员。");
                        return View(model);
                    }
                    else
                    {
                        System.Text.RegularExpressions.Regex regex = new Regex("(\\w)(\\s+)@(\\w)(\\s+)\\.(\\w+)");
                        var match = regex.Match(userExisted.Email);
                        var msg = string.Empty;
                        if (match.Success)
                        {
                            msg = $"{match.Groups[1].Value}{new string('*', match.Groups[2].Value.Length)}@{match.Groups[3].Value}{new string('*', match.Groups[4].Value.Length)}.{match.Groups[5].Value}";
                        }
                        ModelState.AddModelError(nameof(model.Email), $"与激活时填写的地址不符：{msg}");
                        return View(model);
                    }
                }
                else
                {
                    var r = await _userManager.RemovePasswordAsync(userExisted);
                    if (r.Succeeded)
                    {
                        var r2 = await _userManager.AddPasswordAsync(userExisted, model.Password);
                        if (r2.Succeeded)
                        {
                            return RedirectToAction(nameof(Login), new { model.ReturnUrl });
                        }
                        else
                        {
                            _logger.LogWarning("设置用户密码失败：", string.Join(";", r.Errors.Select(x => x.Description)));
                            ModelState.AddModelError("", "设置用户密码失败");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("移除用户密码失败：", string.Join(";", r.Errors.Select(x => x.Description)));
                        ModelState.AddModelError("", "移除用户密码失败");
                    }
                }
            }

            // something went wrong, show form with error
            return View(model);
        }

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                // this is meant to short circuit the UI and only trigger the one external IdP
                return new LoginViewModel
                {
                    EnableLocalLogin = false,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                    ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
                };
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null ||
                            (x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        private async Task<IActionResult> ProcessWindowsLoginAsync(string returnUrl)
        {
            // see if windows auth has already been requested and succeeded
            var result = await HttpContext.AuthenticateAsync(AccountOptions.WindowsAuthenticationSchemeName);
            if (result?.Principal is WindowsPrincipal wp)
            {
                // we will issue the external cookie and then redirect the
                // user back to the external callback, in essence, tresting windows
                // auth the same as any other external authentication mechanism
                var props = new AuthenticationProperties()
                {
                    RedirectUri = Url.Action("ExternalLoginCallback"),
                    Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", AccountOptions.WindowsAuthenticationSchemeName },
                    }
                };

                var id = new ClaimsIdentity(AccountOptions.WindowsAuthenticationSchemeName);
                id.AddClaim(new Claim(JwtClaimTypes.Subject, wp.Identity.Name));
                id.AddClaim(new Claim(JwtClaimTypes.Name, wp.Identity.Name));

                // add the groups as claims -- be careful if the number of groups is too large
                if (AccountOptions.IncludeWindowsGroups)
                {
                    var wi = wp.Identity as WindowsIdentity;
                    var groups = wi.Groups.Translate(typeof(NTAccount));
                    var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
                    id.AddClaims(roles);
                }

                await HttpContext.SignInAsync(
                    IdentityConstants.ExternalScheme,
                    new ClaimsPrincipal(id),
                    props);
                return Redirect(props.RedirectUri);
            }
            else
            {
                // trigger windows auth
                // since windows auth don't support the redirect uri,
                // this URL is re-triggered when we call challenge
                return Challenge(AccountOptions.WindowsAuthenticationSchemeName);
            }
        }

        private async Task<(UserIdentity user, string provider, string providerUserId, IEnumerable<Claim> claims)>
            FindUserFromExternalProviderAsync(AuthenticateResult result)
        {
            var externalUser = result.Principal;

            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            // remove the user id claim so we don't include it as an extra claim if/when we provision the user
            var claims = externalUser.Claims.ToList();
            claims.Remove(userIdClaim);

            var provider = result.Properties.Items["scheme"];
            var providerUserId = userIdClaim.Value;

            // find external user
            var user = await _userManager.FindByLoginAsync(provider, providerUserId);

            return (user, provider, providerUserId, claims);
        }

        private async Task<UserIdentity> AutoProvisionUserAsync(string provider, string providerUserId, IEnumerable<Claim> claims)
        {
            // create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            // user's display name
            var name = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name)?.Value ??
                claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            if (name != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Name, name));
            }
            else
            {
                var first = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ??
                    claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName)?.Value;
                var last = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value ??
                    claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname)?.Value;
                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // email
            var email = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value ??
               claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            if (email != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Email, email));
            }

            var user = new UserIdentity
            {
                UserName = Guid.NewGuid().ToString(),
            };
            var identityResult = await _userManager.CreateAsync(user);
            if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);

            if (filtered.Any())
            {
                identityResult = await _userManager.AddClaimsAsync(user, filtered);
                if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);
            }

            identityResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, provider));
            if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);

            return user;
        }

        private void ProcessLoginCallbackForOidc(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var id_token = externalResult.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }
        }

        private void ProcessLoginCallbackForWsFed(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
        }

        private void ProcessLoginCallbackForSaml2p(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
        }
    }
}