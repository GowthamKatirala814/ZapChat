import { useForm } from "react-hook-form";
import { useDispatch } from "react-redux";
import { useNavigate } from "react-router-dom";

import { login } from "../../api/authApi";
import { loginSuccess } from "../../store/authSlice";

interface LoginForm {
    email: string;
    password: string;
}

export default function LoginPage() {
    const navigate = useNavigate();

    const dispatch = useDispatch();

    const {
        register,
        handleSubmit
    } = useForm<LoginForm>();

    const onSubmit = async (
        data: LoginForm
    ) => {
        try {
            const result =
                await login(data);

            localStorage.setItem(
                "anonymousName",
                result.anonymousName
            );

            localStorage.setItem(
                "email",
                result.email
            );

            dispatch(
                loginSuccess(
                    result.token
                )
            );

            navigate("/dashboard");
        }
        catch {
            alert("Login Failed");
        }
    };

    return (
        <div
            className="
            min-h-screen
            flex
            items-center
            justify-center
            bg-slate-950"
        >
            <form
                onSubmit={
                    handleSubmit(
                        onSubmit
                    )
                }
                className="
                bg-slate-900
                p-8
                rounded-xl
                w-96
                shadow-xl"
            >
                <h1
                    className="
                    text-white
                    text-3xl
                    font-bold
                    mb-6"
                >
                    ZapPulse
                </h1>

                <input
                    {...register(
                        "email"
                    )}
                    placeholder="Email"
                    className="
                    w-full
                    mb-4
                    p-3
                    rounded"
                />

                <input
                    type="password"
                    {...register(
                        "password"
                    )}
                    placeholder="Password"
                    className="
                    w-full
                    mb-4
                    p-3
                    rounded"
                />

                <button
                    className="
                    w-full
                    p-3
                    bg-blue-600
                    rounded
                    text-white"
                >
                    Login
                </button>
            </form>
        </div>
    );
}