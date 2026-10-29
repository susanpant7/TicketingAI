import { useEffect, useRef, useState, type FormEvent } from "react";
import {
  getDepartments,
  type Department,
} from "@/features/departments/departmentsApi";
import {
  deleteDepartmentKnowledge,
  uploadKnowledge,
  type UploadFileResult,
} from "@/features/knowledge/knowledgeApi";
import { errorMessage } from "@/shared/api/errorMessage";
import Alert from "@/shared/components/Alert";
import Button from "@/shared/components/Button";
import ConfirmDialog from "@/shared/components/ConfirmDialog";
import { SelectField } from "@/shared/components/Field";

export default function UploadForm({ onUploaded }: { onUploaded: () => void }) {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [departmentId, setDepartmentId] = useState("");
  const [flush, setFlush] = useState(false);
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [results, setResults] = useState<UploadFileResult[]>([]);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [confirmingRemove, setConfirmingRemove] = useState(false);
  const [removing, setRemoving] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    getDepartments()
      .then(setDepartments)
      .catch((err) =>
        setError(errorMessage(err, "Could not load the departments.")),
      );
  }, []);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setNotice("");
    setResults([]);
    setUploading(true);
    try {
      setResults(await uploadKnowledge(Number(departmentId), flush, files));
      setFiles([]);
      if (fileInput.current) fileInput.current.value = "";
      onUploaded();
    } catch (err) {
      setError(errorMessage(err, "The upload failed. Please try again."));
    } finally {
      setUploading(false);
    }
  }

  const department = departments.find((d) => String(d.id) === departmentId);

  async function handleRemove() {
    setError("");
    setNotice("");
    setResults([]);
    setRemoving(true);
    try {
      const { deleted } = await deleteDepartmentKnowledge(Number(departmentId));
      setNotice(`Removed ${deleted} sections from ${department?.name}.`);
      onUploaded();
    } catch (err) {
      setError(
        errorMessage(err, "Removing the knowledge failed. Please try again."),
      );
    } finally {
      setRemoving(false);
      setConfirmingRemove(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <h2 className="text-base font-semibold text-slate-900">
        Upload knowledge files
      </h2>
      <p className="mt-1 text-sm text-slate-500">
        Each .md or .txt file starts with <code>title:</code> and{" "}
        <code>department:</code> lines, then sections written as{" "}
        <code>## Section title</code> followed by its content. The department in
        the file must match the one selected.
      </p>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <SelectField
          label="Department"
          value={departmentId}
          onChange={(e) => setDepartmentId(e.target.value)}
          required
        >
          <option value="">Select a department</option>
          {departments.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name}
            </option>
          ))}
        </SelectField>
        <div className="flex items-end gap-3">
          <label className="block min-w-0 flex-1 text-sm font-medium text-slate-700">
            Files
            <input
              ref={fileInput}
              type="file"
              multiple
              accept=".md,.txt"
              required
              onChange={(e) => setFiles(Array.from(e.target.files ?? []))}
              className="mt-1.5 block w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-2.5 file:text-sm file:font-medium file:text-slate-700 hover:file:bg-slate-200"
            />
          </label>
          <Button
            variant="dangerGhost"
            disabled={!departmentId || uploading}
            onClick={() => setConfirmingRemove(true)}
          >
            Remove knowledge
          </Button>
        </div>
      </div>

      <fieldset className="mt-4">
        <legend className="text-sm font-medium text-slate-700">Mode</legend>
        <div className="mt-1.5 space-y-1.5 text-sm text-slate-700">
          <label className="flex items-center gap-2">
            <input
              type="radio"
              name="mode"
              checked={!flush}
              onChange={() => setFlush(false)}
            />
            Add: keep the existing knowledge. A file with the same name replaces
            its earlier sections.
          </label>
          <label className="flex items-center gap-2">
            <input
              type="radio"
              name="mode"
              checked={flush}
              onChange={() => setFlush(true)}
            />
            Flush and Add: delete all the existing knowledge first, then add
            these files.
          </label>
        </div>
      </fieldset>

      <div className="mt-4 space-y-3">
        {uploading && (
          <Alert
            variant="success"
            message="Embedding the files. This can take a minute, please keep this page open."
          />
        )}
        {notice && <Alert variant="success" message={notice} />}
        {error && <Alert message={error} />}
        {results.map((r) => (
          <Alert
            key={r.fileName}
            variant={r.error ? "error" : "success"}
            message={
              r.error
                ? `${r.fileName}: ${r.error}`
                : `${r.fileName}: added ${r.sections} sections.`
            }
          />
        ))}
      </div>

      <div className="mt-4">
        <Button
          type="submit"
          disabled={uploading || !departmentId || files.length === 0}
        >
          {uploading ? "Uploading..." : flush ? "Flush and Add" : "Add"}
        </Button>
      </div>
      {confirmingRemove && (
        <ConfirmDialog
          title="Remove knowledge"
          message={`Delete all the knowledge of the ${department?.name} department? This cannot be undone.`}
          confirmLabel="Remove"
          busy={removing}
          onConfirm={handleRemove}
          onCancel={() => setConfirmingRemove(false)}
        />
      )}
    </form>
  );
}
