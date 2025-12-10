// --- CONFIGURAÇÃO DA API ---
const API_URL = "https://localhost:7079/api"; 

// --- GESTÃO DE AUTENTICAÇÃO ---
const AUTH_KEY = "dsx_token";

function saveToken(token) {
  localStorage.setItem(AUTH_KEY, token);
}

function getToken() {
  return localStorage.getItem(AUTH_KEY);
}

function logout() {
  localStorage.removeItem(AUTH_KEY);
  // Como estamos todos na mesma pasta, basta ir para login.html
  window.location.href = "login.html"; 
}

function isLoggedIn() {
  return !!getToken();
}

// Atualiza o Menu (Esconde/Mostra Login ou Logout)
function updateNavAuth() {
  const navList = document.querySelector(".navbar-nav");
  if (!navList) return;

  const existingAuthBtn = document.getElementById("auth-btn-li");
  if (existingAuthBtn) existingAuthBtn.remove();

  const li = document.createElement("li");
  li.id = "auth-btn-li";
  li.className = "nav-item ms-lg-2";

  if (isLoggedIn()) {
    li.innerHTML = `<button class="btn btn-outline-danger btn-sm mt-2 mt-lg-0" onclick="logout()">Logout</button>`;
  } else {
    // Não mostra botão na pagina de login/registo
    if(!window.location.href.includes("login.html") && !window.location.href.includes("register.html")) {
        // Link direto, pois estão na mesma pasta
        li.innerHTML = `<a class="btn btn-outline-primary btn-sm mt-2 mt-lg-0" href="login.html">Login</a>`;
    }
  }
  navList.appendChild(li);
}

// --- LÓGICA DO CARRINHO (Mantém-se igual) ---
document.querySelectorAll("#year").forEach((el) => (el.textContent = new Date().getFullYear()));

const CART_KEY = "dsx_cart";

function getCart() {
  try { return JSON.parse(localStorage.getItem(CART_KEY)) || []; } 
  catch { return []; }
}

function setCart(items) {
  localStorage.setItem(CART_KEY, JSON.stringify(items));
  renderCart();
}

document.addEventListener("click", (e) => {
    if(e.target.matches("[data-add-to-cart]")) {
        const btn = e.target;
        const id = parseInt(btn.getAttribute("data-id"));
        const name = btn.getAttribute("data-name");
        const price = parseFloat(btn.getAttribute("data-price"));
        
        const cart = getCart();
        const existing = cart.find((i) => i.id === id);
        if (existing) existing.qtd += 1;
        else cart.push({ id, name, price, qtd: 1 });
        setCart(cart);
        
        const originalText = btn.textContent;
        btn.classList.remove("btn-primary");
        btn.classList.add("btn-success");
        btn.textContent = "Added!";
        setTimeout(() => {
            btn.classList.remove("btn-success");
            btn.classList.add("btn-primary");
            btn.textContent = originalText;
        }, 1000);
    }
});

function renderCart() {
  const body = document.getElementById("cart-body");
  if (!body) return;
  const cart = getCart();
  body.innerHTML = "";
  let subtotal = 0;

  cart.forEach((item) => {
    const total = item.price * item.qtd;
    subtotal += total;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${item.name}</td>
      <td>€${item.price.toFixed(2)}</td>
      <td>
        <div class="input-group input-group-sm" style="max-width:120px">
          <button class="btn btn-outline-secondary" data-dec>-</button>
          <input class="form-control text-center" value="${item.qtd}" readonly />
          <button class="btn btn-outline-secondary" data-inc>+</button>
        </div>
      </td>
      <td>€${total.toFixed(2)}</td>
      <td><button class="btn btn-sm btn-outline-danger" data-remove><i class="bi bi-trash"></i></button></td>
    `;
    tr.querySelector("[data-dec]").addEventListener("click", () => { item.qtd = Math.max(1, item.qtd - 1); setCart(cart); });
    tr.querySelector("[data-inc]").addEventListener("click", () => { item.qtd += 1; setCart(cart); });
    tr.querySelector("[data-remove]").addEventListener("click", () => {
      const idx = cart.findIndex((i) => i.id === item.id);
      cart.splice(idx, 1);
      setCart(cart);
    });
    body.appendChild(tr);
  });
  const sub = document.getElementById("cart-subtotal");
  if (sub) sub.textContent = `€${subtotal.toFixed(2)}`;
}

document.addEventListener("DOMContentLoaded", () => {
    renderCart();
    updateNavAuth();
});