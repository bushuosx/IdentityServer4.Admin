﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Skoruba.IdentityServer4.Admin.Constants;
using Skoruba.IdentityServer4.Admin.ExceptionHandling;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Services;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Identity;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos.Common;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using NPOI.SS.UserModel;
using System.Collections.Generic;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Dtos;

namespace Skoruba.IdentityServer4.Admin.Controllers
{
    [Authorize(Policy = AuthorizationConsts.AdministrationPolicy)]
    [TypeFilter(typeof(ControllerExceptionFilterAttribute))]
    public class IdentityController : BaseController
    {
        private readonly IIdentityService _identityService;
        private readonly IStringLocalizer<IdentityController> _localizer;

        private static readonly string[] EmployeeStyle = new string[]
        {
            "工号",
            "姓名"
            // ,"身份证号"
        };


        public IdentityController(IIdentityService identityService,
            ILogger<ConfigurationController> logger,
            IStringLocalizer<IdentityController> localizer) : base(logger)
        {
            _identityService = identityService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Roles(int? page, string search)
        {
            ViewBag.Search = search;
            var roles = await _identityService.GetRolesAsync(search, page ?? 1);

            return View(roles);
        }

        [HttpGet]
        [Route("[controller]/[action]")]
        [Route("[controller]/[action]/{id:guid}")]
        public async Task<IActionResult> Role(Guid id)
        {
            if (id == Guid.Empty) return View(new RoleDto());

            var role = await _identityService.GetRoleAsync(new RoleDto { Id = id });

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Role(RoleDto role)
        {
            if (!ModelState.IsValid)
            {
                return View(role);
            }

            if (role.Id == Guid.Empty)
            {
                await _identityService.CreateRoleAsync(role);
            }
            else
            {
                await _identityService.UpdateRoleAsync(role);
            }

            SuccessNotification(string.Format(_localizer["SuccessCreateRole"], role.Name), _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(Roles));
        }

        [HttpGet]
        public async Task<IActionResult> Users(int? page, string search)
        {
            ViewBag.Search = search;
            var usersDto = await _identityService.GetUsersAsync(search, page ?? 1);

            return View(usersDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserProfile(UserDto user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            if (user.Id == Guid.Empty)
            {
                await _identityService.CreateUserAsync(user);
            }
            else
            {
                await _identityService.UpdateUserAsync(user);
            }

            SuccessNotification(string.Format(_localizer["SuccessCreateUser"], user.UserName), _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult UserProfile()
        {
            var newUser = new UserDto();

            return View("UserProfile", newUser);
        }

        [HttpGet]
        [Route("[controller]/UserProfile/{id:guid}")]
        public async Task<IActionResult> UserProfile(Guid id)
        {
            var user = await _identityService.GetUserAsync(new UserDto { Id = id });
            if (user == null) return NotFound();

            return View("UserProfile", user);
        }

        [HttpGet]
        public async Task<IActionResult> UserRoles(Guid id, int? page)
        {
            if (id == Guid.Empty) return NotFound();

            var userRoles = await _identityService.BuildUserRolesViewModel(id, page);

            return View(userRoles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRolesDto role)
        {
            await _identityService.CreateUserRoleAsync(role);
            SuccessNotification(_localizer["SuccessCreateUserRole"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(UserRoles), new { Id = role.UserId });
        }

        [HttpGet]
        public async Task<IActionResult> UserRolesDelete(Guid id, Guid roleId)
        {
            await _identityService.ExistsUserAsync(id);
            await _identityService.ExistsRoleAsync(roleId);

            var roles = await _identityService.GetRolesAsync();

            var rolesDto = new UserRolesDto
            {
                UserId = id,
                RolesList = roles.Select(x => new SelectItem(x.Id.ToString(), x.Name)).ToList(),
                RoleId = roleId
            };

            return View(rolesDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRolesDelete(UserRolesDto role)
        {
            await _identityService.DeleteUserRoleAsync(role);
            SuccessNotification(_localizer["SuccessDeleteUserRole"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(UserRoles), new { Id = role.UserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserClaims(UserClaimsDto claim)
        {
            if (!ModelState.IsValid)
            {
                return View(claim);
            }

            await _identityService.CreateUserClaimsAsync(claim);
            SuccessNotification(string.Format(_localizer["SuccessCreateUserClaims"], claim.ClaimType, claim.ClaimValue), _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(UserClaims), new { Id = claim.UserId });
        }

        [HttpGet]
        public async Task<IActionResult> UserClaims(Guid id, int? page)
        {
            if (id == Guid.Empty) return NotFound();

            var claims = await _identityService.GetUserClaimsAsync(id, page ?? 1);
            claims.UserId = id;

            return View(claims);
        }

        [HttpGet]
        public async Task<IActionResult> UserClaimsDelete(Guid id, int claimId)
        {
            if (id == Guid.Empty || claimId == 0) return NotFound();

            var claim = await _identityService.GetUserClaimAsync(id, claimId);
            if (claim == null) return NotFound();

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserClaimsDelete(UserClaimsDto claim)
        {
            await _identityService.DeleteUserClaimsAsync(claim);
            SuccessNotification(_localizer["SuccessDeleteUserClaims"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(UserClaims), new { Id = claim.UserId });
        }

        [HttpGet]
        public async Task<IActionResult> UserProviders(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var providers = await _identityService.GetUserProvidersAsync(id);

            return View(providers);
        }

        [HttpGet]
        public async Task<IActionResult> UserProvidersDelete(Guid id, string providerKey)
        {
            if (id == Guid.Empty || string.IsNullOrEmpty(providerKey)) return NotFound();

            var provider = await _identityService.GetUserProviderAsync(id, providerKey);
            if (provider == null) return NotFound();

            return View(provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserProvidersDelete(UserProviderDto provider)
        {
            await _identityService.DeleteUserProvidersAsync(provider);
            SuccessNotification(_localizer["SuccessDeleteUserProviders"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(UserProviders), new { Id = provider.UserId });
        }

        [HttpGet]
        public async Task<IActionResult> UserChangePassword(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var user = await _identityService.GetUserAsync(new UserDto { Id = id });
            var userDto = new UserChangePasswordDto { UserId = id, UserName = user.UserName };

            return View(userDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserChangePassword(UserChangePasswordDto userPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(userPassword);
            }

            var identityResult = await _identityService.UserChangePasswordAsync(userPassword);

            if (!identityResult.Errors.Any())
            {
                SuccessNotification(_localizer["SuccessUserChangePassword"], _localizer["SuccessTitle"]);

                return RedirectToAction("UserProfile", new { Id = userPassword.UserId });
            }

            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(userPassword);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleClaims(RoleClaimsDto claim)
        {
            if (!ModelState.IsValid)
            {
                return View(claim);
            }

            await _identityService.CreateRoleClaimsAsync(claim);
            SuccessNotification(string.Format(_localizer["SuccessCreateRoleClaims"], claim.ClaimType, claim.ClaimValue), _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(RoleClaims), new { Id = claim.RoleId });
        }

        [HttpGet]
        public async Task<IActionResult> RoleClaims(Guid id, int? page)
        {
            if (id == Guid.Empty) return NotFound();

            var claims = await _identityService.GetRoleClaimsAsync(id, page ?? 1);
            claims.RoleId = id;

            return View(claims);
        }

        [HttpGet]
        public async Task<IActionResult> RoleClaimsDelete(Guid id, int claimId)
        {
            if (id == Guid.Empty || claimId == 0) return NotFound();

            var claim = await _identityService.GetRoleClaimAsync(id, claimId);

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleClaimsDelete(RoleClaimsDto claim)
        {
            await _identityService.DeleteRoleClaimsAsync(claim);
            SuccessNotification(_localizer["SuccessDeleteRoleClaims"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(RoleClaims), new { Id = claim.RoleId });
        }

        [HttpGet]
        public async Task<IActionResult> RoleDelete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var roleDto = await _identityService.GetRoleAsync(new RoleDto { Id = id });
            if (roleDto == null) return NotFound();

            return View(roleDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleDelete(RoleDto role)
        {
            await _identityService.DeleteRoleAsync(role);
            SuccessNotification(_localizer["SuccessDeleteRole"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserDelete(UserDto user)
        {
            await _identityService.DeleteUserAsync(user);
            SuccessNotification(_localizer["SuccessDeleteUser"], _localizer["SuccessTitle"]);

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> UserDelete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var user = await _identityService.GetUserAsync(new UserDto() { Id = id });
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult ImportUser()
        {
            // return StatusCode(StatusCodes.Status423Locked);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportUser(IFormFile formFile)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await formFile.CopyToAsync(memoryStream);
                IWorkbook workbook = WorkbookFactory.Create(memoryStream);
                ISheet sheet = workbook.GetSheetAt(0);
                IRow row = sheet.GetRow(0);
                for (int i = 0; i < EmployeeStyle.Length; i++)
                {
                    if (row.GetCell(i)?.ToString() != EmployeeStyle[i])
                    {
                        return Content("要导入的数据与预定模板不一致");
                    }
                }

                Dictionary<string, EntityFramework.Entities.Employee> employeeDict = new Dictionary<string, EntityFramework.Entities.Employee>();
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    var tmpRow = sheet.GetRow(i);
                    var gh = tmpRow.GetCell(0)?.ToString();
                    var xm = tmpRow.GetCell(1)?.ToString();
                    var sfhz = tmpRow.GetCell(2)?.ToString();

                    if (string.IsNullOrWhiteSpace(gh)
                         || string.IsNullOrWhiteSpace(xm))
                    {
                        return Content("数据内容有误:工号或姓名不能为空");
                    }

                    if (EmployeeStyle.Length > 2 && string.IsNullOrWhiteSpace(sfhz))
                    {
                        //要求身份证
                        return Content("数据内容有误:身份证号不能为空");
                    }

                    if (!employeeDict.ContainsKey(gh))
                    {
                        employeeDict.Add(gh, new EntityFramework.Entities.Employee { GH_工号 = gh.ToLower(), XM_姓名 = xm.ToLower(), SFZH_身份证号 = sfhz?.ToLower() });
                    }
                }

                if (employeeDict.Count == 0)
                {
                    return Content("操作无效：数据内容为空");
                }

                var employeeList = employeeDict.Values.ToList();

                var rst = await _identityService.ImportUserAsnyc(employeeList);
                if (rst.Count == 0)
                {
                    return Content("导入失败");
                }

                var notAdd = employeeList.Where(x => !rst.Any(y => y.GH_工号 == x.GH_工号)).Select(x => x.GH_工号).ToList();

                if (notAdd.Count == 0)
                {
                    return Content("导入全部成功");
                }
                else
                {
                    var msg = string.Join(";", notAdd);

                    return Content(string.Format("导入部分成功，失败的工号如下：{0}", msg));
                }

            }
        }
    }
}