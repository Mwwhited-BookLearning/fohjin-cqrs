<script setup lang="ts">
import { login } from "../auth/authService";
</script>

<template>
  <div class="login">
    <h1>Fohjin Bank</h1>
    <button @click="login">Sign in</button>
  </div>
</template>
