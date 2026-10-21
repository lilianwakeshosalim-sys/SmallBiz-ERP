const API_BASE_URL = "http://localhost:5149/api";
const BUSINESS_ID = "8a5eea05-947a-48d4-a6b0-5860b3864cde";

async function requestJson(url, options = {}) {
  const response = await fetch(url, options);
  if (!response.ok) {
    const detail = await response.text();
    throw new Error(detail || `Request failed: ${response.status}`);
  }
  return await response.json();
}

export async function getDashboard() {
  return requestJson(`${API_BASE_URL}/dashboard/${BUSINESS_ID}`);
}

export async function getInventory() {
  return requestJson(`${API_BASE_URL}/inventory?businessId=${BUSINESS_ID}`);
}

export async function getCategories() {
  return requestJson(`${API_BASE_URL}/categories?businessId=${BUSINESS_ID}`);
}

export async function createProduct(product) {
  return requestJson(`${API_BASE_URL}/products?businessId=${BUSINESS_ID}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(product),
  });
}
export async function adjustStock(adjustment) {
  return requestJson(`${API_BASE_URL}/stocktransactions/adjust`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      ...adjustment,
      businessId: BUSINESS_ID,
    }),
  });
}
