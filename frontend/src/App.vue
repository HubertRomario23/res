<template>
  <div id="app">
    <header class="app-header">
      <router-link to="/" class="logo">ResultViewer</router-link>
    </header>
    <main class="app-main">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.app-header {
  background: #1a1a2e;
  padding: 1rem 2rem;
}
.logo {
  color: #42b883;
  font-size: 1.4rem;
  font-weight: 700;
  text-decoration: none;
}
.app-main {
  width: 100%;
  max-width: 100%;
  flex: 1;
  padding: 1rem 1.5rem;
  box-sizing: border-box;
}
</style>
