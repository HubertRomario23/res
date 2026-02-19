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
  max-width: 1200px;
  margin: 2rem auto;
  padding: 0 1rem;
}
</style>
