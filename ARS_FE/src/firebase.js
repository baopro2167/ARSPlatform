// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getAnalytics } from "firebase/analytics";
import { getStorage } from "firebase/storage";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
    apiKey: "AIzaSyDkCkXCYSGsBJ0oWVYswB2gX2yKTvMh6Go",
    authDomain: "ars-platform.firebaseapp.com",
    projectId: "ars-platform",
    storageBucket: "ars-platform.firebasestorage.app",
    messagingSenderId: "782594816534",
    appId: "1:782594816534:web:cae410971039499d28d337",
    measurementId: "G-VNEJYNFNXC"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const analytics = getAnalytics(app);
const storage = getStorage(app);

export { storage };