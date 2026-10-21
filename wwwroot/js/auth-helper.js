// js/auth-helper.js

function parseJwt(token) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
}

function getUserInfo() {
  const token = localStorage.getItem('access_token');
  if (!token) return null;

  const decoded = parseJwt(token);
  if (!decoded) return null;

  // Key matching untuk ASP.NET Core JWT Claim Types
  const username =
    decoded.unique_name ||
    decoded.username ||
    decoded.Username ||
    decoded.name ||
    decoded.Name ||
    decoded.email ||
    decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
    decoded.sub ||
    'Pengguna';

  const role =
    decoded.role ||
    decoded.Role ||
    decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
    'User';

  return { username, role, rawPayload: decoded };
}
