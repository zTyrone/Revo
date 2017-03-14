﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using GTRevo.Platform.Security.Identity;

namespace GTRevo.Platform.Security
{
    public class PermissionAuthorizer
    {
        private PermissionCache permissionCache;
        private IRolePermissionResolver rolePermissionResolver;

        public PermissionAuthorizer(PermissionCache permissionCache,
            IRolePermissionResolver rolePermissionResolver)
        {
            this.permissionCache = permissionCache;
            this.rolePermissionResolver = rolePermissionResolver;
        }

        public bool CheckAuthorization(IIdentity identity, IEnumerable<Permission> requiredPermissions)
        {
            PermissionClaimsIdentity permissionClaimsIdentity = identity as PermissionClaimsIdentity;
            ClaimsIdentity claimsIdentity;

            if (permissionClaimsIdentity != null)
            {
                claimsIdentity = permissionClaimsIdentity;

                if (permissionClaimsIdentity.Permissions != null)
                {
                    return Authorize(requiredPermissions, permissionClaimsIdentity.Permissions);
                }
            }
            else
            {
                claimsIdentity = (ClaimsIdentity)identity;
            }

            IEnumerable<Guid> roleIds = claimsIdentity
                .FindAll(claimsIdentity.RoleClaimType)
                .Select(x => Guid.Parse(x.Value));

            HashSet<Permission> permissions = new HashSet<Permission>();

            foreach (Guid roleId in roleIds)
            {
                IEnumerable<Permission> rolePermissions = permissionCache.GetRolePermissions(roleId, rolePermissionResolver);
                foreach (Permission permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            if (permissionClaimsIdentity != null)
            {
                permissionClaimsIdentity.Permissions = permissions;
            }

            return Authorize(requiredPermissions, permissions);
        }
        
        public bool Authorize(IEnumerable<Permission> requiredPermissions,
            HashSet<Permission> availablePermissions)
        {
            //TODO: this needs caching badly!
            PermissionTree permissionTree = new PermissionTree();
            permissionTree.Initialize(availablePermissions);

            foreach (Permission requiredPermission in requiredPermissions)
            {
                if (!permissionTree.AuthorizePermission(requiredPermission))
                {
                    return false;
                }
            }

            return true;
        }
    }
        
}
