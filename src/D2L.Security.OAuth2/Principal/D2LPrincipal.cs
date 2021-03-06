﻿using System;
using System.Collections.Generic;
using System.Linq;
using D2L.Services;
using D2L.Security.OAuth2.Scopes;
using D2L.Security.OAuth2.Validation.AccessTokens;

namespace D2L.Security.OAuth2.Principal {

	internal sealed class D2LPrincipal : ID2LPrincipal {

		private readonly IAccessToken m_accessToken;

		private readonly long? m_userId;
		private readonly long? m_actualUserId;

		private readonly Lazy<Guid> m_tenantId;
		private readonly PrincipalType m_principalType;
		private readonly Lazy<List<Scope>> m_scopes;

		public D2LPrincipal( IAccessToken accessToken ) {
			m_accessToken = accessToken;

			m_tenantId = new Lazy<Guid>( GetTenantId );

			m_scopes = new Lazy<List<Scope>>(
				() => m_accessToken.GetScopes().ToList()
			);

			long userId;
			if ( !m_accessToken.TryGetUserId( out userId ) ) {
				m_principalType = PrincipalType.Service;
				return;
			}

			m_userId = userId;

			long actualUserId;
			if ( !m_accessToken.TryGetActualUserId( out actualUserId ) ) {
				// Doing this means that code that wants to ignore
				// impersonation can do so with less branching.
				m_actualUserId = userId;
				return;
			}

			m_actualUserId = actualUserId;
		}

		long ID2LPrincipal.UserId {
			get {
				AssertPrincipalTypeForClaim( PrincipalType.User, Constants.Claims.USER_ID );

				return m_userId.Value;
			}
		}

		long ID2LPrincipal.ActualUserId {
			get {
				AssertPrincipalTypeForClaim( PrincipalType.User, Constants.Claims.ACTUAL_USER_ID );

				return m_actualUserId.Value;
			}
		}

		Guid ID2LPrincipal.TenantId {
			get { return m_tenantId.Value; }
		}

		PrincipalType ID2LPrincipal.Type {
			get { return m_principalType; }
		}

		IEnumerable<Scope> ID2LPrincipal.Scopes {
			get { return m_scopes.Value; }
		}

		IAccessToken ID2LPrincipal.AccessToken {
			get { return m_accessToken; }
		}
		
		private Guid GetTenantId() {
			string strTenantId = m_accessToken.GetTenantId();

			Guid tenantId;
			if( !Guid.TryParse( strTenantId, out tenantId ) ) {
				string message = string.Format( "TenantId '{0}' is not a valid Guid", strTenantId );
				throw new Exception( message );
			}
			return tenantId;
		}

		private void AssertPrincipalTypeForClaim( PrincipalType type, string claimName ) {
			if ( m_principalType != type ) {
				string message = string.Format(
					"Cannot access {0} for principal type: {1}",
					claimName,
					m_principalType
				);
				throw new InvalidOperationException( message );
			}
		}
	}
}
