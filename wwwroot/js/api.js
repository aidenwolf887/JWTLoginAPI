/**
 * Custom Fetch Interceptor
 * Menggantikan fetch() standar untuk menangani token JWT & Auto Refresh secara otomatis.
 */
async function apiFetch(url, options = {}) {
  // 1. Ambil Access Token dari localStorage
  let token = localStorage.getItem('access_token');

  // 2. Siapkan Headers & Options
  options.headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    options.headers['Authorization'] = `Bearer ${token}`;
  }

  // Wajib 'include' agar cookie refreshToken ikut terkirim ke Backend
  options.credentials = 'include';

  // 3. Jalankan request utama
  let response = await fetch(url, options);

  // 4. Jika dapat status 401 (Unauthorized / Token Expired)
  if (response.status === 401) {
    console.warn('Access Token expired/invalid. Meminta Refresh Token...');

    try {
      // Panggil endpoint refresh-token ke backend
      const refreshResponse = await fetch(
        `${CONFIG.BASE_URL}/auth/refresh-token`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include',
        },
      );

      if (refreshResponse.ok) {
        const refreshData = await refreshResponse.json();
        const newToken =
          refreshData.token ||
          refreshData.accessToken ||
          refreshData.data?.token;

        if (newToken) {
          // Simpan token baru
          localStorage.setItem('access_token', newToken);
          console.log(
            'Refresh Token Berhasil! Mengulangi request sebelumnya...',
          );

          // Update header Authorization dengan token baru
          options.headers['Authorization'] = `Bearer ${newToken}`;

          // Ulangi request asli yang sempat gagal tadi
          return await fetch(url, options);
        }
      }
    } catch (err) {
      console.error('Gagal melakukan Refresh Token:', err);
    }

    // Jika Refresh Token juga gagal/expired -> Paksa Logout
    alert('Sesi Anda telah berakhir. Silakan login kembali.');
    localStorage.removeItem('access_token');
    window.location.href = 'login.html';
    return;
  }

  return response;
}
